using CardMaster.Data;
using CardMaster.Services;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della schermata di condivisione: carica la carta, ne costruisce lo snapshot,
/// lo codifica nel payload di condivisione e ne rende il QR (con fallback se non generabile).
/// </summary>
public sealed class ShareCardViewModel : ObservableObject, IQueryAttributable
{
    private readonly ICardRepository _cards;
    private readonly ICardShareCodec _codec;
    private readonly IBarcodeRenderer _renderer;

    private string _cardId = string.Empty;
    private string _displayName = string.Empty;
    private ImageSource? _qrImage;
    private bool _qrAvailable;
    private bool _loaded;

    public ShareCardViewModel(ICardRepository cards, ICardShareCodec codec, IBarcodeRenderer renderer)
    {
        _cards = cards;
        _codec = codec;
        _renderer = renderer;
    }

    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    public ImageSource? QrImage
    {
        get => _qrImage;
        private set => SetProperty(ref _qrImage, value);
    }

    /// <summary>Vero se il QR è stato generato (immagine disponibile).</summary>
    public bool QrAvailable
    {
        get => _qrAvailable;
        private set
        {
            if (SetProperty(ref _qrAvailable, value))
            {
                OnPropertyChanged(nameof(QrUnavailable));
            }
        }
    }

    /// <summary>Vero se il QR NON è generabile (mostra messaggio di fallback).</summary>
    public bool QrUnavailable => !_qrAvailable;

    /// <summary>Esito del caricamento: false se la carta non esiste (la pagina torna indietro).</summary>
    public bool CardExists { get; private set; }

    public async Task LoadAsync()
    {
        if (_loaded)
        {
            return;
        }
        _loaded = true;

        var card = string.IsNullOrEmpty(_cardId) ? null : await _cards.GetByIdAsync(_cardId);
        if (card is null)
        {
            CardExists = false;
            return;
        }

        CardExists = true;
        DisplayName = card.DisplayName;

        var snapshot = new CardShareSnapshot(
            DisplayName: card.DisplayName,
            IssuerName: card.IssuerName,
            Barcode: card.Barcode,
            BarcodeFormat: card.BarcodeFormat,
            Color: card.Color,
            LogoId: card.LogoId);

        var payload = _codec.Encode(snapshot);
        var result = _renderer.Render(payload, BarcodeFormatCatalog.QrCode);
        QrImage = result.Image;
        QrAvailable = result.Succeeded;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is not null)
        {
            _cardId = id.ToString() ?? string.Empty;
        }
    }
}
