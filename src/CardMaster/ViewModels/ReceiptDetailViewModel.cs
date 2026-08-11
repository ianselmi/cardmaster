using System.Globalization;
using CardMaster.Data;
using CardMaster.Services.Receipts;

namespace CardMaster.ViewModels;

/// <summary>
/// Dettaglio in sola lettura di uno scontrino: dati di testata, immagine se conservata,
/// testo riconosciuto consultabile.
/// </summary>
public sealed class ReceiptDetailViewModel : ObservableObject
{
    private readonly IReceiptRepository _repository;
    private readonly IReceiptImageStore _imageStore;

    private Receipt? _receipt;
    private string _merchant = string.Empty;
    private string _date = string.Empty;
    private string _total = string.Empty;
    private string _vatId = string.Empty;
    private string _rawText = string.Empty;
    private ImageSource? _image;

    public ReceiptDetailViewModel(IReceiptRepository repository, IReceiptImageStore imageStore)
    {
        _repository = repository;
        _imageStore = imageStore;
    }

    public string? ReceiptId => _receipt?.Id;

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

        var full = _imageStore.ResolveFullPath(receipt.ImagePath);
        Image = full is null ? null : ImageSource.FromFile(full);
        OnPropertyChanged(nameof(ShowImageMissing));
        return true;
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
