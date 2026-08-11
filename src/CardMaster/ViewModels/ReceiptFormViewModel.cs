using System.Collections.ObjectModel;
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
    private readonly ICategoryCatalog _categories;
    private readonly IProductMappingRepository _mappings;

    private Receipt? _existing;
    private string? _pendingImagePath;
    private ReceiptVatSummary _vatSummary = ReceiptVatSummary.Empty;
    private long? _taxCents;

    private string _merchantName = string.Empty;
    private string _merchantVatId = string.Empty;
    private DateTime _purchaseDate = DateTime.Today;
    private bool _hasDate;
    private string _totalText = string.Empty;
    private string _rawText = string.Empty;
    private string _balanceMessage = string.Empty;
    private bool _balanceIsOk;

    public ReceiptFormViewModel(
        IReceiptRepository repository,
        IReceiptImageStore imageStore,
        ISettingsStore settings,
        ICategoryCatalog categories,
        IProductMappingRepository mappings)
    {
        _repository = repository;
        _imageStore = imageStore;
        _settings = settings;
        _categories = categories;
        _mappings = mappings;

        Items.CollectionChanged += (_, _) => UpdateBalance();
    }

    /// <summary>Righe dello scontrino, modificabili una per una.</summary>
    public ObservableCollection<ReceiptItemViewModel> Items { get; } = [];

    /// <summary>Categorie selezionabili, con la voce vuota per "senza categoria".</summary>
    public ObservableCollection<string> CategoryNames { get; } = [];

    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Esito della quadratura, in cima alla pagina. Se il totale torna l'utente conferma senza
    /// scorrere le righe: un carrello ha decine di voci, e chiedere di verificarle una per una
    /// farebbe abbandonare la funzione al terzo scontrino.
    /// </summary>
    public string BalanceMessage
    {
        get => _balanceMessage;
        private set => SetProperty(ref _balanceMessage, value);
    }

    public bool BalanceIsOk
    {
        get => _balanceIsOk;
        private set => SetProperty(ref _balanceIsOk, value);
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
        set
        {
            if (SetProperty(ref _totalText, value))
            {
                UpdateBalance();
            }
        }
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
    public void InitializeFromCapture(ReceiptHeader header, string rawText, string? imagePath) =>
        InitializeFromCapture(header, rawText, imagePath, ReceiptItemsResult.None);

    /// <summary>Prepara il form per uno scontrino appena acquisito, righe comprese.</summary>
    public void InitializeFromCapture(
        ReceiptHeader header,
        string rawText,
        string? imagePath,
        ReceiptItemsResult items)
    {
        _existing = null;
        _pendingImagePath = imagePath;
        _vatSummary = items.VatSummary;
        _taxCents = header.TaxCents;
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

        _pendingLines = items.Items;
    }

    /// <summary>Righe appena ricostruite, in attesa di essere classificate e mostrate.</summary>
    private IReadOnlyList<ReceiptItemLine> _pendingLines = [];

    /// <summary>
    /// Carica categorie e mappature apprese e popola le righe. Sta fuori dal costruttore perché
    /// legge dal database e dal bundle: il form deve poter comparire prima che finisca.
    /// </summary>
    public async Task LoadCategoriesAsync()
    {
        var catalog = await _categories.GetAllAsync().ConfigureAwait(true);
        var learned = await _mappings.GetLearnedAsync().ConfigureAwait(true);

        if (CategoryNames.Count == 0)
        {
            CategoryNames.Add(NoCategory);
            foreach (var category in catalog)
            {
                CategoryNames.Add(category.Name);
            }
        }

        // Le righe già salvate portano l'id della categoria: qui diventa il nome che l'utente
        // vede nel selettore, senza che la rilettura conti come una sua correzione.
        foreach (var item in Items)
        {
            var byId = catalog.FirstOrDefault(c => c.Id == item.Category);
            if (byId is not null)
            {
                item.SetCategoryQuietly(byId.Name);
            }
        }

        if (_pendingLines.Count == 0)
        {
            UpdateBalance();
            return;
        }

        foreach (var line in _pendingLines)
        {
            var id = CategoryMatcher.Resolve(line.RawDescription, learned, catalog);
            var name = catalog.FirstOrDefault(c => c.Id == id)?.Name;
            Add(ReceiptItemViewModel.FromLine(line, name));
        }

        _pendingLines = [];
        UpdateBalance();
    }

    /// <summary>Voce del selettore per una riga che resta senza categoria.</summary>
    public const string NoCategory = "— senza categoria —";

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
        _taxCents = receipt.TaxCents;
        RawText = receipt.RawText;

        Items.Clear();
        foreach (var item in await _repository.GetItemsAsync(id).ConfigureAwait(true))
        {
            Add(ReceiptItemViewModel.FromEntity(item));
        }

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
        UpdateBalance();
        return true;
    }

    /// <summary>Aggiunge una riga che il riconoscimento non ha letto.</summary>
    public void AddEmptyItem() => Add(ReceiptItemViewModel.Empty());

    /// <summary>Elimina una riga che il riconoscimento ha inventato.</summary>
    public void RemoveItem(ReceiptItemViewModel item)
    {
        item.Changed -= OnItemChanged;
        Items.Remove(item);
    }

    private void Add(ReceiptItemViewModel item)
    {
        item.Changed += OnItemChanged;
        Items.Add(item);
    }

    private void OnItemChanged(object? sender, EventArgs e) => UpdateBalance();

    /// <summary>
    /// Ricalcola la quadratura a ogni modifica: l'utente deve vedere l'effetto della correzione
    /// mentre la fa, non dopo aver salvato.
    /// </summary>
    private void UpdateBalance()
    {
        OnPropertyChanged(nameof(HasItems));

        if (Items.Count == 0)
        {
            BalanceIsOk = false;
            BalanceMessage = "Nessuna riga letta da questo scontrino. Puoi aggiungerle a mano.";
            return;
        }

        var lines = Items.Select(i => i.ToLine()).ToList();
        var balance = ReceiptTotalsCheck.Verify(lines, ParseTotalCents(TotalText), _vatSummary);

        BalanceIsOk = balance.Status == ReceiptBalanceStatus.Balanced &&
                      balance.RateStatus != ReceiptBalanceStatus.Mismatch;

        BalanceMessage = BuildBalanceMessage(balance);
    }

    private static string BuildBalanceMessage(ReceiptBalance balance)
    {
        var lines = ReceiptListViewModel.FormatCents(balance.LinesTotalCents);

        if (balance.Status == ReceiptBalanceStatus.NotChecked)
        {
            return $"Righe per {lines}. Senza totale non c'è niente con cui confrontarle.";
        }

        if (balance.Status == ReceiptBalanceStatus.Mismatch)
        {
            var gap = ReceiptListViewModel.FormatCents(Math.Abs(balance.DifferenceCents));
            var direction = balance.DifferenceCents > 0 ? "in più" : "in meno";
            return $"Le righe non tornano con il totale: {lines}, cioè {gap} {direction}. Controlla le righe.";
        }

        if (balance.RateStatus == ReceiptBalanceStatus.Mismatch)
        {
            var rates = string.Join(
                ", ",
                balance.UnbalancedRates.Select(r => $"{r.RateBasisPoints / 100m:0.##}%"));
            return $"Il totale torna ({lines}), ma non l'IVA: controlla le righe al {rates}.";
        }

        return balance.RateStatus == ReceiptBalanceStatus.Balanced
            ? $"Le righe tornano con il totale e con l'IVA: {lines}."
            : $"Le righe tornano con il totale: {lines}.";
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
            _existing.TaxCents = _taxCents;
            await _repository.UpdateAsync(_existing).ConfigureAwait(true);
            await SaveItemsAsync(_existing.Id).ConfigureAwait(true);
            return;
        }

        var receipt = new Receipt
        {
            MerchantName = Nullify(MerchantName),
            MerchantVatId = Nullify(MerchantVatId),
            PurchasedAt = purchasedAt,
            TotalCents = totalCents,
            TaxCents = _taxCents,
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
        await SaveItemsAsync(receipt.Id).ConfigureAwait(true);
    }

    /// <summary>
    /// Salva le righe sostituendole in blocco, e trasforma in mappature apprese le sole
    /// categorie che l'utente ha <b>cambiato</b>: valgono da qui in avanti, e non riscrivono
    /// gli scontrini già salvati.
    /// </summary>
    private async Task SaveItemsAsync(string receiptId)
    {
        var entities = new List<ReceiptItem>();
        var catalog = await _categories.GetAllAsync().ConfigureAwait(true);

        foreach (var item in Items)
        {
            if (string.IsNullOrWhiteSpace(item.Description) && item.AmountCents == 0)
            {
                // Riga aggiunta e lasciata vuota: non è un dato, è un ripensamento.
                continue;
            }

            var entity = item.ToEntity(receiptId);
            entity.Category = catalog.FirstOrDefault(c => c.Name == item.Category)?.Id;
            entities.Add(entity);

            if (item.CategoryChangedByUser && entity.Category is not null)
            {
                await _mappings
                    .SetAsync(entity.NormalizedDescription, entity.Category, ProductMappingOrigin.User)
                    .ConfigureAwait(true);
            }
        }

        await _repository.ReplaceItemsAsync(receiptId, entities).ConfigureAwait(true);
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
