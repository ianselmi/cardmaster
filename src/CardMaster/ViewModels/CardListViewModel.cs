using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
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
    private string _emptyStateTitle = string.Empty;
    private string _emptyStateSubtitle = string.Empty;
    private bool _isUpdateAvailable;

    public CardListViewModel(ICardRepository cards, IUpdateService updateService, ISettingsStore settings)
    {
        _cards = cards;
        _updateService = updateService;
        _settings = settings;

        // Il servizio è singleton per tutta la vita dell'app: segue il controllo automatico
        // (App.xaml.cs) e quello manuale avviato dalla pagina Impostazioni/Controllo aggiornamenti.
        _updateService.StateChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshUpdateBadge);
        RefreshUpdateBadge();
    }

    /// <summary>Carte mostrate nella griglia principale, filtrate da <see cref="SearchText"/>.</summary>
    public ObservableCollection<Card> FilteredCards { get; } = new();

    /// <summary>Ultime carte aperte (al più <see cref="RecentCardsCount"/>), più recente prima.</summary>
    public ObservableCollection<Card> RecentCards { get; } = new();

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

    /// <summary>Totale carte a riposo ("30 carte"), trovate/totale durante il filtro ("5/30").</summary>
    public string CountText
    {
        get => _countText;
        private set => SetProperty(ref _countText, value);
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

    /// <summary>Versione remota rilevata, per il testo del banner; valido solo mentre <see cref="IsUpdateAvailable"/> è vero.</summary>
    public string? UpdateAvailableVersion => _updateService.LastCheckedRelease?.VersionName ?? _settings.LastUpdateCheckAvailableVersion;

    /// <summary>Chiude il banner/badge per la versione corrente, senza toccare il flusso di download in `UpdatePage`.</summary>
    public void DismissUpdateBanner()
    {
        if (UpdateAvailableVersion is null)
        {
            return;
        }

        _settings.UpdateNotifyDismissedVersion = UpdateAvailableVersion;
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

    private void ApplyFilter()
    {
        var query = Normalize(SearchText);

        var matches = string.IsNullOrEmpty(query)
            ? _allCards
            : _allCards
                .Where(c => Normalize(c.DisplayName).Contains(query) || Normalize(c.IssuerName).Contains(query))
                .ToList();

        FilteredCards.Clear();
        foreach (var card in matches)
        {
            FilteredCards.Add(card);
        }

        CountText = string.IsNullOrEmpty(query)
            ? FormatTotal(_allCards.Count)
            : $"{FilteredCards.Count}/{_allCards.Count}";

        if (_allCards.Count == 0)
        {
            EmptyStateTitle = "Nessuna carta ancora";
            EmptyStateSubtitle = "Le carte scansionate compariranno qui.";
        }
        else
        {
            EmptyStateTitle = "Nessuna carta trovata";
            EmptyStateSubtitle = "Prova un altro nome o emittente.";
        }
    }

    private static string FormatTotal(int count) => count == 1 ? "1 carta" : $"{count} carte";

    /// <summary>Normalizza per un confronto case/accent-insensitive (es. "citta" trova "Città").</summary>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
