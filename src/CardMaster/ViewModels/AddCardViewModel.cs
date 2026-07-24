using System.Collections.ObjectModel;
using CardMaster.Data;
using CardMaster.Services;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della schermata di conferma/modifica carta (scansione o inserimento manuale).
/// Riceve barcode e formato via query di navigazione; consente di scegliere l'emittente
/// (catalogo/libero/nessuno) con arricchimento, valida e salva.
/// </summary>
public sealed class AddCardViewModel : ObservableObject, IQueryAttributable
{
    public const string NoneLabel = "Nessuno";
    public const string OtherLabel = "Altro…";

    private readonly ICardRepository _cards;
    private readonly IIssuerCatalog _catalog;
    private readonly Dictionary<string, Issuer> _issuersByName = new(StringComparer.OrdinalIgnoreCase);

    private string _barcode = string.Empty;
    private string? _selectedFormat;
    private string _displayName = string.Empty;
    private string? _colorHex;
    private string? _logoId;
    private string _selectedIssuerOption = NoneLabel;
    private string _customIssuerName = string.Empty;
    private bool _isCustomIssuer;

    public AddCardViewModel(ICardRepository cards, IIssuerCatalog catalog)
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
    }
}
