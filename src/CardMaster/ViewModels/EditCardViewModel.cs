using System.Collections.ObjectModel;
using CardMaster.Data;
using CardMaster.Services;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della schermata di modifica di una carta esistente. Carica la carta per Id,
/// consente di correggere nome, emittente (catalogo/libero/nessuno, con ri-arricchimento) e
/// formato; il valore del barcode è immutabile (sola lettura). Salva via
/// <see cref="ICardRepository.UpdateAsync"/> ed elimina (tombstone) via
/// <see cref="ICardRepository.SoftDeleteAsync"/>.
/// </summary>
public sealed class EditCardViewModel : ObservableObject, IQueryAttributable
{
    public const string NoneLabel = "Nessuno";
    public const string OtherLabel = "Altro…";

    private readonly ICardRepository _cards;
    private readonly IIssuerCatalog _catalog;
    private readonly Dictionary<string, Issuer> _issuersByName = new(StringComparer.OrdinalIgnoreCase);

    private Card? _card;
    private string _cardId = string.Empty;
    private string _barcodeValue = string.Empty;
    private string? _selectedFormat;
    private string _displayName = string.Empty;
    private string? _colorHex;
    private string? _logoId;
    private string _selectedIssuerOption = NoneLabel;
    private string _customIssuerName = string.Empty;
    private bool _isCustomIssuer;
    private bool _initialized;
    private bool _loaded;

    public EditCardViewModel(ICardRepository cards, IIssuerCatalog catalog)
    {
        _cards = cards;
        _catalog = catalog;
    }

    public IReadOnlyList<string> Formats { get; } = BarcodeFormatCatalog.Supported;

    public ObservableCollection<string> IssuerOptions { get; } = new() { NoneLabel };

    /// <summary>Valore del barcode: immutabile, mostrato in sola lettura.</summary>
    public string BarcodeValue
    {
        get => _barcodeValue;
        private set => SetProperty(ref _barcodeValue, value);
    }

    public string? SelectedFormat
    {
        get => _selectedFormat;
        set => SetProperty(ref _selectedFormat, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string SelectedIssuerOption
    {
        get => _selectedIssuerOption;
        set
        {
            if (SetProperty(ref _selectedIssuerOption, value))
            {
                ApplyIssuerSelection(value);
            }
        }
    }

    public string CustomIssuerName
    {
        get => _customIssuerName;
        set => SetProperty(ref _customIssuerName, value);
    }

    public bool IsCustomIssuer
    {
        get => _isCustomIssuer;
        private set => SetProperty(ref _isCustomIssuer, value);
    }

    /// <summary>Esito del caricamento: false se la carta non esiste (la pagina torna indietro).</summary>
    public bool CardExists { get; private set; }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        var issuers = await _catalog.GetAllAsync();
        foreach (var issuer in issuers)
        {
            _issuersByName[issuer.Name] = issuer;
            IssuerOptions.Add(issuer.Name);
        }

        IssuerOptions.Add(OtherLabel);
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

        _card = card;
        CardExists = true;

        // Baseline dai dati correnti della carta (preservati se non si cambia emittente).
        BarcodeValue = card.Barcode;
        DisplayName = card.DisplayName;
        SelectedFormat = string.IsNullOrWhiteSpace(card.BarcodeFormat) ? null : card.BarcodeFormat;
        _colorHex = card.Color;
        _logoId = card.LogoId;

        // Pre-selezione dell'opzione emittente corrente (catalogo / libero / nessuno).
        if (string.IsNullOrWhiteSpace(card.IssuerName))
        {
            SelectedIssuerOption = NoneLabel;
        }
        else if (_issuersByName.ContainsKey(card.IssuerName))
        {
            SelectedIssuerOption = card.IssuerName;
        }
        else
        {
            CustomIssuerName = card.IssuerName;
            SelectedIssuerOption = OtherLabel;
        }
    }

    private void ApplyIssuerSelection(string option)
    {
        IsCustomIssuer = option == OtherLabel;

        if (option == NoneLabel || option == OtherLabel)
        {
            return;
        }

        if (!_issuersByName.TryGetValue(option, out var issuer))
        {
            return;
        }

        // Arricchimento dal catalogo (come in creazione): non sovrascrive campi già valorizzati.
        _colorHex = issuer.ColorHex;
        _logoId = issuer.LogoAsset;

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = issuer.Name;
        }

        if (string.IsNullOrWhiteSpace(SelectedFormat) && !string.IsNullOrWhiteSpace(issuer.ExpectedBarcodeFormat))
        {
            SelectedFormat = issuer.ExpectedBarcodeFormat;
        }
    }

    /// <summary>Emittente finale (dal catalogo, libero, o null). Calcolato al salvataggio.</summary>
    private string? ResolveIssuerName()
    {
        if (SelectedIssuerOption == NoneLabel)
        {
            return null;
        }

        if (SelectedIssuerOption == OtherLabel)
        {
            return string.IsNullOrWhiteSpace(CustomIssuerName) ? null : CustomIssuerName.Trim();
        }

        return SelectedIssuerOption;
    }

    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            error = "Inserisci un nome per la carta.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedFormat))
        {
            error = "Seleziona il formato del codice.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public async Task SaveAsync()
    {
        if (_card is null)
        {
            return;
        }

        // Preserva Id, CreatedAt e Barcode (immutabile); aggiorna solo i campi editabili.
        _card.DisplayName = DisplayName.Trim();
        _card.BarcodeFormat = SelectedFormat!;
        _card.IssuerName = ResolveIssuerName();
        _card.Color = _colorHex;
        _card.LogoId = _logoId;

        await _cards.UpdateAsync(_card);
    }

    public Task DeleteAsync() => _cards.SoftDeleteAsync(_cardId);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is not null)
        {
            _cardId = id.ToString() ?? string.Empty;
        }
    }
}
