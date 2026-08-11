using System.Collections.ObjectModel;
using System.Globalization;
using CardMaster.Data;
using CardMaster.Services.Receipts;

namespace CardMaster.ViewModels;

/// <summary>Riga dello scontrino come si legge nel dettaglio: la stessa tabella del cartaceo.</summary>
/// <param name="Description">Descrizione stampata.</param>
/// <param name="Quantity">Quantità, vuota quando è una sola unità.</param>
/// <param name="Vat">Aliquota in percentuale, <b>vuota</b> quando non è stata letta.</param>
/// <param name="Amount">Importo formattato in euro.</param>
/// <param name="Category">Categoria, vuota se la riga non è classificata.</param>
public readonly record struct ReceiptDetailItem(
    string Description,
    string Quantity,
    string Vat,
    string Amount,
    string Category)
{
    public bool HasQuantity => Quantity.Length > 0;

    public bool HasCategory => Category.Length > 0;
}

/// <summary>
/// Dettaglio in sola lettura di uno scontrino: dati di testata, immagine se conservata,
/// testo riconosciuto consultabile.
/// </summary>
public sealed class ReceiptDetailViewModel : ObservableObject
{
    private readonly IReceiptRepository _repository;
    private readonly IReceiptImageStore _imageStore;
    private readonly ICategoryCatalog _categories;

    private Receipt? _receipt;
    private string _balanceMessage = string.Empty;
    private string _merchant = string.Empty;
    private string _date = string.Empty;
    private string _total = string.Empty;
    private string _vatId = string.Empty;
    private string _rawText = string.Empty;
    private ImageSource? _image;

    public ReceiptDetailViewModel(
        IReceiptRepository repository,
        IReceiptImageStore imageStore,
        ICategoryCatalog categories)
    {
        _repository = repository;
        _imageStore = imageStore;
        _categories = categories;
    }

    public string? ReceiptId => _receipt?.Id;

    /// <summary>Righe dello scontrino, in sola lettura.</summary>
    public ObservableCollection<ReceiptDetailItem> Items { get; } = [];

    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Esito della quadratura. Vuoto per uno scontrino senza righe — acquisito prima che l'app
    /// le leggesse — che resta consultabile come sempre, senza sezioni vuote né errori.
    /// </summary>
    public string BalanceMessage
    {
        get => _balanceMessage;
        private set => SetProperty(ref _balanceMessage, value);
    }

    public string Merchant
    {
        get => _merchant;
        private set => SetProperty(ref _merchant, value);
    }

    public string Date
    {
        get => _date;
        private set => SetProperty(ref _date, value);
    }

    public string Total
    {
        get => _total;
        private set => SetProperty(ref _total, value);
    }

    public string VatId
    {
        get => _vatId;
        private set
        {
            if (SetProperty(ref _vatId, value))
            {
                OnPropertyChanged(nameof(HasVatId));
            }
        }
    }

    public bool HasVatId => VatId.Length > 0;

    public string RawText
    {
        get => _rawText;
        private set
        {
            if (SetProperty(ref _rawText, value))
            {
                OnPropertyChanged(nameof(HasRawText));
            }
        }
    }

    public bool HasRawText => RawText.Length > 0;

    /// <summary>Immagine dello scontrino, null se non conservata o già eliminata.</summary>
    public ImageSource? Image
    {
        get => _image;
        private set
        {
            if (SetProperty(ref _image, value))
            {
                OnPropertyChanged(nameof(HasImage));
                OnPropertyChanged(nameof(ImageMissingMessage));
                OnPropertyChanged(nameof(ShowImageMissing));
            }
        }
    }

    public bool HasImage => Image is not null;

    public bool ShowImageMissing => _receipt is not null && Image is null;

    /// <summary>
    /// Spiega l'assenza dell'immagine invece di lasciare un vuoto: il caso più frequente è
    /// il ripristino di un backup, che riporta i dati ma non le immagini.
    /// </summary>
    public string ImageMissingMessage =>
        "Immagine non disponibile: non è stata conservata, è stata eliminata per liberare spazio, " +
        "oppure lo scontrino viene da un backup (le immagini non sono comprese nel backup).";

    public async Task<bool> LoadAsync(string id)
    {
        var receipt = await _repository.GetByIdAsync(id).ConfigureAwait(true);
        if (receipt is null)
        {
            return false;
        }

        _receipt = receipt;
        Merchant = string.IsNullOrWhiteSpace(receipt.MerchantName)
            ? "Esercente non riconosciuto"
            : receipt.MerchantName;
        Date = receipt.PurchasedAt?.ToString("dddd d MMMM yyyy", ReceiptListViewModel.Italian) ?? "Data mancante";
        Total = ReceiptListViewModel.FormatCents(receipt.TotalCents);
        VatId = receipt.MerchantVatId ?? string.Empty;
        RawText = receipt.RawText;

        await LoadItemsAsync(receipt).ConfigureAwait(true);

        var full = _imageStore.ResolveFullPath(receipt.ImagePath);
        Image = full is null ? null : ImageSource.FromFile(full);
        OnPropertyChanged(nameof(ShowImageMissing));
        return true;
    }

    private async Task LoadItemsAsync(Receipt receipt)
    {
        Items.Clear();

        var stored = await _repository.GetItemsAsync(receipt.Id).ConfigureAwait(true);
        if (stored.Count == 0)
        {
            BalanceMessage = string.Empty;
            OnPropertyChanged(nameof(HasItems));
            return;
        }

        var catalog = await _categories.GetAllAsync().ConfigureAwait(true);

        foreach (var item in stored)
        {
            Items.Add(new ReceiptDetailItem(
                item.Description,
                FormatQuantity(item),
                item.VatRateBasisPoints is null
                    ? string.Empty
                    : (item.VatRateBasisPoints.Value / 100m).ToString("0.##", ReceiptListViewModel.Italian) + "%",
                ReceiptListViewModel.FormatCents(item.AmountCents),
                catalog.FirstOrDefault(c => c.Id == item.Category)?.Name ?? string.Empty));
        }

        var sum = stored.Sum(i => i.AmountCents);
        BalanceMessage = BuildBalanceMessage(sum, receipt.TotalCents);
        OnPropertyChanged(nameof(HasItems));
    }

    /// <summary>
    /// La quadratura resta un'affermazione sullo scontrino salvato: dice se le righe conservate
    /// tornano con il totale, senza rifare il riconoscimento e senza correggere niente.
    /// </summary>
    private static string BuildBalanceMessage(long sum, long? total)
    {
        if (total is null)
        {
            return $"Righe per {ReceiptListViewModel.FormatCents(sum)}. Senza totale non sono verificabili.";
        }

        var difference = sum - total.Value;
        if (difference == 0)
        {
            return $"Le righe tornano con il totale: {ReceiptListViewModel.FormatCents(sum)}.";
        }

        var gap = ReceiptListViewModel.FormatCents(Math.Abs(difference));
        return $"Le righe non tornano con il totale: {ReceiptListViewModel.FormatCents(sum)}, cioè {gap} " +
               (difference > 0 ? "in più." : "in meno.");
    }

    /// <summary>Quantità mostrata solo quando dice qualcosa: una unità non si scrive.</summary>
    private static string FormatQuantity(ReceiptItem item)
    {
        if (item.Unit == ReceiptItemUnit.Piece && item.QuantityMilli == ReceiptItemLine.SingleUnit)
        {
            return string.Empty;
        }

        var quantity = (item.QuantityMilli / 1000m).ToString("0.###", ReceiptListViewModel.Italian);
        return item.Unit == ReceiptItemUnit.Kilogram ? $"{quantity} kg" : $"{quantity} pz";
    }

    /// <summary>
    /// Cancellazione logica dello scontrino; l'immagine viene rimossa dal device, perché
    /// tenerla occuperebbe spazio per un dato che l'utente non vede più.
    /// </summary>
    public async Task DeleteAsync()
    {
        if (_receipt is null)
        {
            return;
        }

        _imageStore.Delete(_receipt.ImagePath);
        await _repository.DeleteAsync(_receipt.Id).ConfigureAwait(true);
    }
}
