using System.Collections.ObjectModel;
using CardMaster.Data;
using CardMaster.Services;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della schermata di conferma/modifica carta (scansione o inserimento manuale).
/// Riceve barcode e formato via query di navigazione; consente di scegliere l'emittente
/// (catalogo/libero/nessuno) con arricchimento, il colore del riquadro e le label
/// (entrambi opzionali, da <see cref="CardFormViewModel"/>), valida e salva.
/// </summary>
public sealed class AddCardViewModel : CardFormViewModel, IQueryAttributable
{
    public const string NoneLabel = "Nessuno";
    public const string OtherLabel = "Altro…";

    private readonly ICardRepository _cards;
    private readonly IIssuerCatalog _catalog;
    private readonly Dictionary<string, Issuer> _issuersByName = new(StringComparer.OrdinalIgnoreCase);

    private string _barcode = string.Empty;
    private string? _selectedFormat;
    private string? _colorHex;
    private string? _logoId;
    private string _selectedIssuerOption = NoneLabel;
    private string _customIssuerName = string.Empty;
    private bool _isCustomIssuer;

    // Snapshot ricevuto via QR di condivisione: risolto dopo InitializeAsync
    // (ApplyQueryAttributes può arrivare prima che il catalogo sia caricato).
    private string? _pendingIssuerName;
    private string? _receivedColor;
    private string? _receivedLogo;
    private bool _pendingIssuerResolved;

    public AddCardViewModel(ICardRepository cards, IIssuerCatalog catalog)
        : base(cards)
    {
        _cards = cards;
        _catalog = catalog;
    }

    public IReadOnlyList<string> Formats { get; } = BarcodeFormatCatalog.Supported;

    public ObservableCollection<string> IssuerOptions { get; } = new() { NoneLabel };

    public string Barcode
    {
        get => _barcode;
        set => SetProperty(ref _barcode, value);
    }

    public string? SelectedFormat
    {
        get => _selectedFormat;
        set => SetProperty(ref _selectedFormat, value);
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

    public async Task InitializeAsync()
    {
        if (_issuersByName.Count > 0)
        {
            return;
        }

        var issuers = await _catalog.GetAllAsync();
        foreach (var issuer in issuers)
        {
            _issuersByName[issuer.Name] = issuer;
            IssuerOptions.Add(issuer.Name);
        }

        IssuerOptions.Add(OtherLabel);

        ResolvePendingIssuer();

        await LoadLabelSuggestionsAsync();
    }

    /// <summary>
    /// Applica l'emittente ricevuto da uno snapshot condiviso, ora che il catalogo è
    /// caricato: match col catalogo → opzione corrispondente; altrimenti emittente libero.
    /// I valori di colore/logo RICEVUTI vincono sull'arricchimento del catalogo.
    /// </summary>
    private void ResolvePendingIssuer()
    {
        if (_pendingIssuerResolved)
        {
            return;
        }
        _pendingIssuerResolved = true;

        if (!string.IsNullOrWhiteSpace(_pendingIssuerName))
        {
            if (_issuersByName.TryGetValue(_pendingIssuerName, out var issuer))
            {
                SelectedIssuerOption = issuer.Name; // opzione del catalogo (arricchisce colore/logo/nome)
            }
            else
            {
                CustomIssuerName = _pendingIssuerName;
                SelectedIssuerOption = OtherLabel;
            }
        }

        // I valori ricevuti hanno la precedenza sull'arricchimento del catalogo.
        if (!string.IsNullOrWhiteSpace(_receivedColor))
        {
            _colorHex = _receivedColor;
        }

        if (!string.IsNullOrWhiteSpace(_receivedLogo))
        {
            _logoId = _receivedLogo;
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

        // Arricchimento dal catalogo.
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

    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Barcode))
        {
            error = "Inserisci il codice a barre.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedFormat))
        {
            error = "Seleziona il formato del codice.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            error = "Inserisci un nome per la carta.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public Task<bool> BarcodeExistsAsync() => _cards.AnyActiveByBarcodeAsync(Barcode.Trim());

    public async Task SaveAsync()
    {
        var card = new Card
        {
            DisplayName = DisplayName.Trim(),
            Barcode = Barcode.Trim(),
            BarcodeFormat = SelectedFormat!,
            IssuerName = ResolveIssuerName(),
            Color = _colorHex,
            LogoId = _logoId,
            TileColor = SelectedTileColor,
            Labels = Labels.ToList(),
        };

        await _cards.AddAsync(card);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("barcode", out var barcode) && barcode is not null)
        {
            Barcode = barcode.ToString() ?? string.Empty;
        }

        if (query.TryGetValue("format", out var format) && format is not null)
        {
            SelectedFormat = format.ToString();
        }

        // Campi aggiuntivi presenti solo in ricezione da un QR di condivisione.
        if (query.TryGetValue("name", out var name) && name is not null)
        {
            DisplayName = name.ToString() ?? string.Empty;
        }

        if (query.TryGetValue("issuer", out var issuer) && issuer is not null)
        {
            _pendingIssuerName = issuer.ToString();
        }

        if (query.TryGetValue("color", out var color) && color is not null)
        {
            _receivedColor = color.ToString();
            _colorHex = _receivedColor;
        }

        if (query.TryGetValue("logo", out var logo) && logo is not null)
        {
            _receivedLogo = logo.ToString();
            _logoId = _receivedLogo;
        }
    }
}
