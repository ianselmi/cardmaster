using System.Collections.ObjectModel;
using CardMaster.Data;
using CardMaster.Services;
using CardMaster.Views;

namespace CardMaster.ViewModels;

/// <summary>
/// Base comune alle due schermate che compilano una carta (creazione e modifica):
/// scelta del colore del riquadro e gestione delle label. Sta qui — e non duplicata nei
/// due ViewModel — perché le viste riusabili (<c>CardColorPickerView</c>,
/// <c>CardLabelEditorView</c>) possano bindare a una sola forma di API.
/// </summary>
public abstract class CardFormViewModel : ObservableObject
{
    private readonly ICardRepository _cards;

    /// <summary>Tutte le label già in uso nell'app, per i suggerimenti.</summary>
    private readonly List<string> _knownLabels = new();

    private string _displayName = string.Empty;
    private string _newLabelText = string.Empty;
    private string? _labelWarning;
    private bool _suggestionsLoaded;

    protected CardFormViewModel(ICardRepository cards)
    {
        _cards = cards;

        AddLabelCommand = new Command(() => AddLabel(NewLabelText));
        RemoveLabelCommand = new Command<string>(RemoveLabel);
        AddSuggestionCommand = new Command<string>(AddLabel);
        SelectColorCommand = new Command<TileColorOption>(SelectColor);

        BuildColorOptions();
    }

    /// <summary>Nome mostrato: sta nella base perché guida l'anteprima del colore "Automatico".</summary>
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (SetProperty(ref _displayName, value))
            {
                RefreshAutoColorPreview();
            }
        }
    }

    // ---- Colore del riquadro -------------------------------------------------

    /// <summary>Pastiglia "Automatico" più una per ogni colore della palette.</summary>
    public ObservableCollection<TileColorOption> ColorOptions { get; } = new();

    public Command<TileColorOption> SelectColorCommand { get; }

    /// <summary>Colore scelto dall'utente da persistere in <c>Card.TileColor</c> (null = automatico).</summary>
    protected string? SelectedTileColor =>
        ColorOptions.FirstOrDefault(o => o.IsSelected) is { IsAuto: false } chosen ? chosen.Hex : null;

    private void BuildColorOptions()
    {
        ColorOptions.Add(TileColorOption.Auto(CardTilePalette.ForName(DisplayName)));
        foreach (var color in CardTilePalette.Colors)
        {
            ColorOptions.Add(TileColorOption.FromPalette(color));
        }

        ColorOptions[0].IsSelected = true;
    }

    /// <summary>Pre-seleziona il colore corrente della carta (null o non valido = automatico).</summary>
    protected void SetTileColor(string? hex)
    {
        var match = string.IsNullOrWhiteSpace(hex)
            ? null
            : ColorOptions.FirstOrDefault(o => !o.IsAuto && HexEquals(o.Hex, hex));

        // Colore valido ma fuori palette (es. scritto da una versione futura): lo si tiene
        // come opzione aggiuntiva invece di riportare silenziosamente la carta ad "Automatico".
        if (match is null && !string.IsNullOrWhiteSpace(hex) && CardTilePalette.TryParse(hex, out var custom))
        {
            match = TileColorOption.FromPalette(custom);
            ColorOptions.Insert(1, match);
        }

        SelectColor(match ?? ColorOptions[0]);
    }

    private void SelectColor(TileColorOption? option)
    {
        if (option is null)
        {
            return;
        }

        foreach (var candidate in ColorOptions)
        {
            candidate.IsSelected = ReferenceEquals(candidate, option);
        }
    }

    private void RefreshAutoColorPreview()
    {
        var auto = ColorOptions.FirstOrDefault(o => o.IsAuto);
        if (auto is not null)
        {
            auto.Color = CardTilePalette.ForName(DisplayName);
        }
    }

    private static bool HexEquals(string? a, string? b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    // ---- Label ---------------------------------------------------------------

    /// <summary>Label assegnate alla carta in questa schermata.</summary>
    public ObservableCollection<string> Labels { get; } = new();

    /// <summary>Label già usate su altre carte e non ancora assegnate a questa.</summary>
    public ObservableCollection<string> LabelSuggestions { get; } = new();

    public Command AddLabelCommand { get; }

    public Command<string> RemoveLabelCommand { get; }

    public Command<string> AddSuggestionCommand { get; }

    /// <summary>Testo in composizione nell'editor delle label.</summary>
    public string NewLabelText
    {
        get => _newLabelText;
        set => SetProperty(ref _newLabelText, value);
    }

    public bool HasLabels => Labels.Count > 0;

    public bool HasLabelSuggestions => LabelSuggestions.Count > 0;

    /// <summary>Avviso non bloccante (es. limite di label raggiunto); null quando non c'è nulla da dire.</summary>
    public string? LabelWarning
    {
        get => _labelWarning;
        private set
        {
            if (SetProperty(ref _labelWarning, value))
            {
                OnPropertyChanged(nameof(HasLabelWarning));
            }
        }
    }

    public bool HasLabelWarning => !string.IsNullOrEmpty(LabelWarning);

    /// <summary>Aggiunge una label applicando normalizzazione, dedup e limite per carta.</summary>
    public void AddLabel(string? raw)
    {
        var candidate = CardLabels.Normalize(raw);
        var labels = Labels.ToList();

        switch (CardLabels.TryAdd(labels, candidate))
        {
            case AddLabelResult.Added:
                Labels.Add(candidate);
                NewLabelText = string.Empty;
                LabelWarning = null;
                RefreshSuggestions();
                OnPropertyChanged(nameof(HasLabels));
                break;

            case AddLabelResult.LimitReached:
                LabelWarning = $"Massimo {CardLabels.MaxPerCard} label per carta.";
                break;

            case AddLabelResult.Duplicate:
                // Già presente (anche con maiuscole o accenti diversi): si svuota il campo
                // senza segnalare un errore — l'intento dell'utente è comunque soddisfatto.
                NewLabelText = string.Empty;
                LabelWarning = null;
                break;

            case AddLabelResult.Empty:
                NewLabelText = string.Empty;
                break;
        }
    }

    public void RemoveLabel(string? label)
    {
        if (string.IsNullOrEmpty(label) || !Labels.Remove(label))
        {
            return;
        }

        LabelWarning = null;
        RefreshSuggestions();
        OnPropertyChanged(nameof(HasLabels));
    }

    /// <summary>Carica le label già in uso nell'app per proporle come suggerimenti.</summary>
    protected async Task LoadLabelSuggestionsAsync()
    {
        if (_suggestionsLoaded)
        {
            return;
        }
        _suggestionsLoaded = true;

        var cards = await _cards.GetAllAsync();
        foreach (var label in cards.SelectMany(c => c.Labels))
        {
            if (!_knownLabels.Any(k => CardLabels.AreSame(k, label)))
            {
                _knownLabels.Add(label);
            }
        }

        RefreshSuggestions();
    }

    /// <summary>Pre-carica le label correnti della carta (senza toccare i suggerimenti già noti).</summary>
    protected void SetLabels(IEnumerable<string> labels)
    {
        Labels.Clear();
        foreach (var label in CardLabels.Sanitize(labels))
        {
            Labels.Add(label);
        }

        RefreshSuggestions();
        OnPropertyChanged(nameof(HasLabels));
    }

    private void RefreshSuggestions()
    {
        var available = _knownLabels
            .Where(k => !Labels.Any(l => CardLabels.AreSame(l, k)))
            .OrderBy(TextNormalizer.Normalize, StringComparer.Ordinal)
            .ToList();

        LabelSuggestions.Clear();
        foreach (var label in available)
        {
            LabelSuggestions.Add(label);
        }

        OnPropertyChanged(nameof(HasLabelSuggestions));
    }
}
