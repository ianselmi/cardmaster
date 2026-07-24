using System.Collections.ObjectModel;
using CardMaster.Data;
using CardMaster.Services;

namespace CardMaster.ViewModels;

/// <summary>
/// ViewModel della lista carte. In maui-shell è un segnaposto funzionale:
/// legge dal repository (di norma vuoto) e popola la collezione osservabile.
/// L'aggiunta/scansione delle carte arriva con maui-scan-card.
/// </summary>
public sealed class CardListViewModel
{
    private readonly ICardRepository _cards;

    public CardListViewModel(ICardRepository cards)
    {
        _cards = cards;
    }

    public ObservableCollection<Card> Cards { get; } = new();

    public async Task LoadAsync()
    {
        var items = await _cards.GetAllAsync();

        Cards.Clear();
        foreach (var card in items)
        {
            Cards.Add(card);
        }
    }
}
