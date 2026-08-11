using System.Globalization;
using CardMaster.Data;
using CardMaster.Services;
using CardMaster.Services.Receipts;

namespace CardMaster.ViewModels;

/// <summary>
/// Schermata di conferma e correzione dei dati di testata, usata sia per uno scontrino
/// appena acquisito sia per modificarne uno già salvato.
/// <para>
/// Ogni campo non riconosciuto arriva vuoto e viene segnalato come tale: il parser non
/// inventa valori, ed è questo che rende sensato chiedere all'utente di controllare.
/// </para>
/// </summary>
public sealed class ReceiptFormViewModel : ObservableObject
{
    private readonly IReceiptRepository _repository;
    private readonly IReceiptImageStore _imageStore;
    private readonly ISettingsStore _settings;

    private Receipt? _existing;
    private string? _pendingImagePath;

    private string _merchantName = string.Empty;
    private string _merchantVatId = string.Empty;
    private DateTime _purchaseDate = DateTime.Today;
    private bool _hasDate;
    private string _totalText = string.Empty;
    private string _rawText = string.Empty;

    public ReceiptFormViewModel(
        IReceiptRepository repository,
        IReceiptImageStore imageStore,
        ISettingsStore settings)
    {
        _repository = repository;
        _imageStore = imageStore;
        _settings = settings;
    }

    public string MerchantName
    {
        get => _merchantName;
        set => SetProperty(ref _merchantName, value);
    }

    public string MerchantVatId
    {
        get => _merchantVatId;
        set => SetProperty(ref _merchantVatId, value);
    }

    public DateTime PurchaseDate
    {
        get => _purchaseDate;
        set => SetProperty(ref _purchaseDate, value);
    }

    /// <summary>
    /// Falso quando la data non è stata riconosciuta: il selettore mostra comunque oggi,
    /// ma finché l'utente non conferma la data lo scontrino resta senza data.
    /// </summary>
    public bool HasDate
    {
        get => _hasDate;
        set => SetProperty(ref _hasDate, value);
    }

    public string TotalText
    {
        get => _totalText;
        set => SetProperty(ref _totalText, value);
    }

    public string RawText
    {
        get => _rawText;
        private set => SetProperty(ref _rawText, value);
    }

    /// <summary>Elenco leggibile dei campi rimasti vuoti dopo il riconoscimento.</summary>
    public string NotRecognizedMessage { get; private set; } = string.Empty;

    public bool HasNotRecognized => NotRecognizedMessage.Length > 0;

    public bool IsEditingExisting => _existing is not null;

    /// <summary>Prepara il form per uno scontrino appena acquisito.</summary>
    public void InitializeFromCapture(ReceiptHeader header, string rawText, string? imagePath)
    {
        _existing = null;
        _pendingImagePath = imagePath;
        RawText = rawText;

        MerchantName = header.MerchantName ?? string.Empty;
        MerchantVatId = header.MerchantVatId ?? string.Empty;
        TotalText = header.TotalCents is null
            ? string.Empty
            : (header.TotalCents.Value / 100m).ToString("0.00", ReceiptListViewModel.Italian);

        HasDate = header.PurchasedAt is not null;
        PurchaseDate = header.PurchasedAt?.DateTime.Date ?? DateTime.Today;

        var missing = new List<string>();
        if (header.MerchantName is null) missing.Add("esercente");
        if (header.PurchasedAt is null) missing.Add("data");
        if (header.TotalCents is null) missing.Add("totale");

        NotRecognizedMessage = missing.Count == 0
            ? string.Empty
            : $"Non riconosciuto: {string.Join(", ", missing)}. Compila a mano prima di salvare.";

        OnPropertyChanged(nameof(NotRecognizedMessage));
        OnPropertyChanged(nameof(HasNotRecognized));
        OnPropertyChanged(nameof(IsEditingExisting));
    }

    /// <summary>Carica uno scontrino esistente per la modifica.</summary>
    public async Task<bool> LoadExistingAsync(string id)
    {
        var receipt = await _repository.GetByIdAsync(id).ConfigureAwait(true);
        if (receipt is null)
        {
            return false;
        }

        _existing = receipt;
        _pendingImagePath = null;
        RawText = receipt.RawText;

        MerchantName = receipt.MerchantName ?? string.Empty;
        MerchantVatId = receipt.MerchantVatId ?? string.Empty;
        TotalText = receipt.TotalCents is null
            ? string.Empty
            : (receipt.TotalCents.Value / 100m).ToString("0.00", ReceiptListViewModel.Italian);
        HasDate = receipt.PurchasedAt is not null;
        PurchaseDate = receipt.PurchasedAt?.DateTime.Date ?? DateTime.Today;

        NotRecognizedMessage = string.Empty;
        OnPropertyChanged(nameof(NotRecognizedMessage));
        OnPropertyChanged(nameof(HasNotRecognized));
        OnPropertyChanged(nameof(IsEditingExisting));
        return true;
    }

    /// <summary>
    /// Valida il solo totale: uno scontrino senza data o senza esercente resta salvabile
    /// (finisce fra gli incompleti), ma un totale scritto male sarebbe un dato falso.
    /// </summary>
    public bool Validate(out string error)
    {
        error = string.Empty;
        if (TotalText.Length > 0 && ParseTotalCents(TotalText) is null)
        {
            error = "Il totale non è un importo valido. Usa il formato 12,34.";
            return false;
        }

        return true;
    }

    public async Task SaveAsync()
    {
        var totalCents = ParseTotalCents(TotalText);

        // Ancorata a mezzanotte UTC, non all'offset locale. La data d'acquisto è una data di
        // calendario, non un istante: sqlite-net persiste il DateTimeOffset come tick UTC e lo
        // rilegge come UTC, quindi con offset +02:00 la mezzanotte italiana tornava indietro
        // alle 22:00 del giorno prima — ogni scontrino risultava di un giorno prima, e a cavallo
        // di fine mese sarebbe finito nel mese sbagliato. Visto su emulatore l'11 ago 2026.
        // SpecifyKind obbligatorio: il DatePicker restituisce un DateTime con Kind=Local, e il
        // costruttore di DateTimeOffset rifiuta un offset diverso da quello locale per quel Kind
        // (ArgumentException a runtime). Qui la data è un giorno di calendario, senza fuso.
        var purchasedAt = HasDate
            ? new DateTimeOffset(DateTime.SpecifyKind(PurchaseDate.Date, DateTimeKind.Unspecified), TimeSpan.Zero)
            : (DateTimeOffset?)null;

        if (_existing is not null)
        {
            _existing.MerchantName = Nullify(MerchantName);
            _existing.MerchantVatId = Nullify(MerchantVatId);
            _existing.PurchasedAt = purchasedAt;
            _existing.TotalCents = totalCents;
            await _repository.UpdateAsync(_existing).ConfigureAwait(true);
            return;
        }

        var receipt = new Receipt
        {
            MerchantName = Nullify(MerchantName),
            MerchantVatId = Nullify(MerchantVatId),
            PurchasedAt = purchasedAt,
            TotalCents = totalCents,
            RawText = RawText,
        };

        // L'immagine si conserva solo se l'utente lo vuole. Se la copia fallisce lo scontrino
        // si salva comunque: perdere l'immagine è un fastidio, perdere i dati no.
        if (_settings.KeepReceiptImages && _pendingImagePath is not null)
        {
            receipt.ImagePath = await _imageStore
                .SaveAsync(_pendingImagePath, receipt.Id)
                .ConfigureAwait(true);
        }

        await _repository.AddAsync(receipt).ConfigureAwait(true);
    }

    private static string? Nullify(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Importo digitato dall'utente in centesimi interi. Accetta virgola e punto come
    /// separatore decimale: sulla tastiera numerica Android si trovano entrambi.
    /// </summary>
    public static long? ParseTotalCents(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ||
            value < 0)
        {
            return null;
        }

        return (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
    }
}
