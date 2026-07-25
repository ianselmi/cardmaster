using CardMaster.Data;
using CardMaster.Services;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della pagina di visualizzazione carta: carica la carta, ne rende il barcode
/// (con fallback se non generabile) ed espone i dati per la UI.
/// </summary>
public sealed class ShowCardViewModel : ObservableObject, IQueryAttributable
{
    private readonly ICardRepository _cards;
    private readonly IBarcodeRenderer _renderer;

    private string _cardId = string.Empty;
    private string _displayName = string.Empty;
    private string? _issuerName;
    private string _barcodeValue = string.Empty;
    private ImageSource? _barcodeImage;
    private bool _barcodeAvailable;
    private bool _loaded;
    private bool _lastUsedTouched;

    public ShowCardViewModel(ICardRepository cards, IBarcodeRenderer renderer)
    {
        _cards = cards;
        _renderer = renderer;
    }

    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    public string? IssuerName
    {
        get => _issuerName;
        private set => SetProperty(ref _issuerName, value);
    }

    /// <summary>Valore del barcode, sempre mostrato in chiaro.</summary>
    public string BarcodeValue
    {
        get => _barcodeValue;
        private set => SetProperty(ref _barcodeValue, value);
    }

    public ImageSource? BarcodeImage
    {
        get => _barcodeImage;
        private set => SetProperty(ref _barcodeImage, value);
    }

    /// <summary>Vero se il barcode è stato generato (immagine disponibile).</summary>
    public bool BarcodeAvailable
    {
        get => _barcodeAvailable;
        private set
        {
            if (SetProperty(ref _barcodeAvailable, value))
            {
                OnPropertyChanged(nameof(BarcodeUnavailable));
            }
        }
    }

    /// <summary>Vero se il barcode NON è generabile (mostra messaggio di fallback).</summary>
    public bool BarcodeUnavailable => !_barcodeAvailable;

    /// <summary>Esito del caricamento: false se la carta non esiste (la pagina torna indietro).</summary>
    public bool CardExists { get; private set; }

    /// <summary>Id della carta corrente (per modifica/eliminazione).</summary>
    public string CardId => _cardId;

    /// <summary>Ricarica i dati della carta (usato al ritorno dalla modifica).</summary>
    public Task ReloadAsync()
    {
        _loaded = false;
        return LoadAsync();
    }

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
        IssuerName = card.IssuerName;
        BarcodeValue = card.Barcode;

        var result = _renderer.Render(card.Barcode, card.BarcodeFormat);
        BarcodeImage = result.Image;
        BarcodeAvailable = result.Succeeded;

        // "Uso" = prima apertura riuscita di questa pagina. _loaded viene resettato da
        // ReloadAsync per ricaricare i dati dopo una modifica, ma quel reload non è un
        // nuovo "uso": _lastUsedTouched non viene mai resettato, quindi il touch scatta una sola volta.
        if (!_lastUsedTouched)
        {
            _lastUsedTouched = true;
            await _cards.TouchLastUsedAsync(_cardId);
        }
    }

    /// <summary>Eliminazione logica (tombstone) della carta corrente.</summary>
    public Task DeleteAsync() => _cards.SoftDeleteAsync(_cardId);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is not null)
        {
            _cardId = id.ToString() ?? string.Empty;
        }
    }
}
