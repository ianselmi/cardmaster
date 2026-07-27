using System.Collections.ObjectModel;
using CardMaster.Data;
using CardMaster.Services;
using CardMaster.Services.Update;
using Microsoft.Maui.ApplicationModel;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della lista carte: carica le carte attive dal repository, espone la
/// ricerca testuale (nome/emittente, case/accent-insensitive) e la barra delle
/// carte usate di recente.
/// </summary>
public sealed class CardListViewModel : ObservableObject
{
    private const int RecentCardsCount = 3;

    private readonly ICardRepository _cards;
    private readonly IUpdateService _updateService;
    private readonly ISettingsStore _settings;
    private List<Card> _allCards = new();
    private string _searchText = string.Empty;
    private string _countText = string.Empty;
    private bool _hasRecentCards;
    private bool _hasLabelFilters;
    private bool _hasActiveFilter;
    private string _emptyStateTitle = string.Empty;
    private string _emptyStateSubtitle = string.Empty;
    private bool _isUpdateAvailable;

    public CardListViewModel(ICardRepository cards, IUpdateService updateService, ISettingsStore settings)
    {
        _cards = cards;
        _updateService = updateService;
        _settings = settings;

        ToggleLabelFilterCommand = new Command<LabelFilterItem>(ToggleLabelFilter);

        // Il servizio è singleton per tutta la vita dell'app: segue il controllo automatico
        // (App.xaml.cs) e quello manuale avviato dalla pagina Impostazioni/Controllo aggiornamenti.
        _updateService.StateChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshUpdateBadge);
        RefreshUpdateBadge();
    }

    /// <summary>Carte mostrate nella griglia principale, filtrate da <see cref="SearchText"/>.</summary>
    public ObservableCollection<Card> FilteredCards { get; } = new();

    /// <summary>Ultime carte aperte (al più <see cref="RecentCardsCount"/>), più recente prima.</summary>
    public ObservableCollection<Card> RecentCards { get; } = new();

    /// <summary>Chip del filtro per label: una per label in uso su almeno una carta attiva.</summary>
    public ObservableCollection<LabelFilterItem> LabelFilters { get; } = new();

    /// <summary>Attiva/disattiva un chip del filtro (selezione multipla, in OR).</summary>
    public Command<LabelFilterItem> ToggleLabelFilterCommand { get; }

    /// <summary>Vero se esiste almeno una label: sotto questa soglia la riga di chip non si mostra.</summary>
    public bool HasLabelFilters
    {
        get => _hasLabelFilters;
        private set
        {
            if (SetProperty(ref _hasLabelFilters, value))
            {
                OnPropertyChanged(nameof(HasFilterRow));
            }
        }
    }

    /// <summary>
    /// Vero se la riga che ospita conteggio e chip ha qualcosa da mostrare. Serve sul
    /// contenitore, non solo sui figli: un contenitore con altezza fissa occuperebbe spazio
    /// anche con entrambi i figli invisibili.
    /// </summary>
    public bool HasFilterRow => HasLabelFilters || HasActiveFilter;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>Vero se esiste almeno una carta usata di recente (mostra la barra dedicata).</summary>
    public bool HasRecentCards
    {
        get => _hasRecentCards;
        private set => SetProperty(ref _hasRecentCards, value);
    }

    /// <summary>Carte visibili sul totale durante il filtro ("5/30"). Vuoto a riposo.</summary>
    public string CountText
    {
        get => _countText;
        private set => SetProperty(ref _countText, value);
    }

    /// <summary>
    /// Vero solo quando un filtro è attivo (testo o label). A riposo il conteggio non si mostra:
    /// chi sta guardando le proprie carte le sta già vedendo, e la riga costerebbe spazio alla griglia.
    /// </summary>
    public bool HasActiveFilter
    {
        get => _hasActiveFilter;
        private set
        {
            if (SetProperty(ref _hasActiveFilter, value))
            {
                OnPropertyChanged(nameof(HasFilterRow));
            }
        }
    }

    /// <summary>Titolo dello stato vuoto: distingue "nessuna carta salvata" da "nessun risultato".</summary>
    public string EmptyStateTitle
    {
        get => _emptyStateTitle;
        private set => SetProperty(ref _emptyStateTitle, value);
    }

    public string EmptyStateSubtitle
    {
        get => _emptyStateSubtitle;
        private set => SetProperty(ref _emptyStateSubtitle, value);
    }

    /// <summary>Vero se un controllo (manuale o automatico) ha rilevato un aggiornamento non ancora silenziato dall'utente.</summary>
    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set => SetProperty(ref _isUpdateAvailable, value);
    }

    /// <summary>
    /// Versione remota da installare, per il testo del banner; valido solo mentre
    /// <see cref="IsUpdateAvailable"/> è vero. Già filtrata dal servizio rispetto alla versione
    /// installata: dopo aver installato l'aggiornamento non annuncia più quella versione.
    /// </summary>
    public string? UpdateAvailableVersion => _updateService.AvailableUpdateVersion;

    /// <summary>Chiude il banner/badge per la versione corrente, senza toccare il flusso di download in `UpdatePage`.</summary>
    public void DismissUpdateBanner()
    {
        if (UpdateAvailableVersion is null)
        {
            return;
        }

        _settings.UpdateNotifyDismissedVersion = UpdateAvailableVersion;

        // Chiudere il banner silenzia la versione per entrambi i canali, notifica compresa.
        _updateService.CancelUpdateNotification();

        RefreshUpdateBadge();
    }

    public async Task LoadAsync()
    {
        _allCards = await _cards.GetAllAsync();

        var recent = await _cards.GetRecentlyUsedAsync(RecentCardsCount);
        RecentCards.Clear();
        foreach (var card in recent)
        {
            RecentCards.Add(card);
        }
        HasRecentCards = RecentCards.Count > 0;

        RebuildLabelFilters();
        ApplyFilter();

        // Rilegge lo stato al ritorno da Impostazioni/Controllo aggiornamenti (es. dopo un dismiss).
        RefreshUpdateBadge();
    }

    private void RefreshUpdateBadge()
    {
        var availableVersion = UpdateAvailableVersion;
        IsUpdateAvailable = availableVersion is not null
            && !string.Equals(availableVersion, _settings.UpdateNotifyDismissedVersion, StringComparison.Ordinal);
        OnPropertyChanged(nameof(UpdateAvailableVersion));
    }

    /// <summary>
    /// Ricostruisce i chip dalle label delle carte attive, conservando le selezioni attive
    /// e potando quelle rimaste orfane (label che nessuna carta usa più): un filtro attivo
    /// senza il suo chip svuoterebbe la griglia senza niente a cui attribuirlo.
    /// </summary>
    private void RebuildLabelFilters()
    {
        var selected = LabelFilters
            .Where(f => f.IsSelected)
            .Select(f => f.Name)
            .ToList();

        var labels = _allCards
            .SelectMany(c => c.Labels)
            .GroupBy(Normalize)
            .Select(g => g.First())
            .OrderBy(Normalize, StringComparer.Ordinal)
            .ToList();

        LabelFilters.Clear();
        foreach (var label in labels)
        {
            var wasSelected = selected.Any(s => CardLabels.AreSame(s, label));
            LabelFilters.Add(new LabelFilterItem(label, wasSelected));
        }

        HasLabelFilters = LabelFilters.Count > 0;
    }

    private void ToggleLabelFilter(LabelFilterItem? item)
    {
        if (item is null)
        {
            return;
        }

        item.IsSelected = !item.IsSelected;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = Normalize(SearchText);
        var selectedLabels = LabelFilters
            .Where(f => f.IsSelected)
            .Select(f => Normalize(f.Name))
            .ToList();

        // Testo e label si combinano in AND; tra le label selezionate vale l'OR.
        var matches = _allCards
            .Where(c => string.IsNullOrEmpty(query)
                || Normalize(c.DisplayName).Contains(query)
                || Normalize(c.IssuerName).Contains(query))
            .Where(c => selectedLabels.Count == 0
                || c.Labels.Any(l => selectedLabels.Contains(Normalize(l))))
            .ToList();

        FilteredCards.Clear();
        foreach (var card in matches)
        {
            FilteredCards.Add(card);
        }

        HasActiveFilter = !string.IsNullOrEmpty(query) || selectedLabels.Count > 0;

        // A riposo il conteggio non si mostra affatto: niente testo da calcolare.
        CountText = HasActiveFilter ? $"{FilteredCards.Count}/{_allCards.Count}" : string.Empty;

        if (_allCards.Count == 0)
        {
            EmptyStateTitle = "Nessuna carta ancora";
            EmptyStateSubtitle = "Le carte scansionate compariranno qui.";
        }
        else
        {
            EmptyStateTitle = "Nessuna carta trovata";
            EmptyStateSubtitle = selectedLabels.Count > 0
                ? "Prova a togliere qualche label o a cambiare la ricerca."
                : "Prova un altro nome o emittente.";
        }
    }

    /// <summary>Normalizza per un confronto case/accent-insensitive (es. "citta" trova "Città").</summary>
    private static string Normalize(string? value) => TextNormalizer.Normalize(value);
}
