using System.Collections.ObjectModel;
using System.Globalization;
using CardMaster.Data;
using CardMaster.Services.Receipts;

namespace CardMaster.ViewModels;

/// <summary>Riga della lista scontrini, già formattata per la vista.</summary>
public sealed class ReceiptListItem
{
    public required string Id { get; init; }

    public required string Merchant { get; init; }

    public required string Date { get; init; }

    public required string Total { get; init; }

    /// <summary>Vero se manca data o totale: escluso dai totali, da completare.</summary>
    public required bool IsIncomplete { get; init; }
}

/// <summary>Quanto è stato speso presso un esercente nel mese corrente.</summary>
public sealed class MerchantSpendItem
{
    public required string Merchant { get; init; }

    public required string Amount { get; init; }
}

/// <summary>
/// Lista degli scontrini e spesa del mese corrente, ripartita per esercente.
/// Aggregazione sui soli dati di testata: non serve conoscere le righe dei prodotti.
/// </summary>
public sealed class ReceiptListViewModel : ObservableObject
{
    /// <summary>
    /// Formattazione fissa italiana: l'app è in italiano e gli importi sono in euro. Usare la
    /// cultura del device mostrerebbe "$0.00" su un telefono configurato in inglese — visto
    /// sull'emulatore l'11 ago 2026.
    /// </summary>
    public static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");

    private readonly IReceiptRepository _repository;

    private string _monthTotal = string.Empty;
    private string _monthLabel = string.Empty;
    private int _incompleteCount;
    private bool _isEmpty = true;

    public ReceiptListViewModel(IReceiptRepository repository)
    {
        _repository = repository;
    }

    public ObservableCollection<ReceiptListItem> Receipts { get; } = [];

    public ObservableCollection<MerchantSpendItem> MonthByMerchant { get; } = [];

    /// <summary>Totale speso nel mese corrente, formattato.</summary>
    public string MonthTotal
    {
        get => _monthTotal;
        private set => SetProperty(ref _monthTotal, value);
    }

    /// <summary>Nome del mese corrente, per intestare la vista di spesa.</summary>
    public string MonthLabel
    {
        get => _monthLabel;
        private set => SetProperty(ref _monthLabel, value);
    }

    /// <summary>Quanti scontrini sono esclusi dai totali perché privi di data o totale.</summary>
    public int IncompleteCount
    {
        get => _incompleteCount;
        private set
        {
            if (SetProperty(ref _incompleteCount, value))
            {
                OnPropertyChanged(nameof(HasIncomplete));
                OnPropertyChanged(nameof(IncompleteMessage));
            }
        }
    }

    public bool HasIncomplete => IncompleteCount > 0;

    public string IncompleteMessage => IncompleteCount == 1
        ? "1 scontrino è escluso dai totali perché manca la data o il totale."
        : $"{IncompleteCount} scontrini sono esclusi dai totali perché manca la data o il totale.";

    /// <summary>Vero quando non è ancora stato acquisito alcuno scontrino.</summary>
    public bool IsEmpty
    {
        get => _isEmpty;
        private set
        {
            if (SetProperty(ref _isEmpty, value))
            {
                OnPropertyChanged(nameof(HasReceipts));
            }
        }
    }

    public bool HasReceipts => !IsEmpty;

    public async Task LoadAsync()
    {
        var receipts = await _repository.GetAllAsync().ConfigureAwait(true);

        Receipts.Clear();
        foreach (var receipt in receipts)
        {
            Receipts.Add(new ReceiptListItem
            {
                Id = receipt.Id,
                Merchant = string.IsNullOrWhiteSpace(receipt.MerchantName)
                    ? "Esercente non riconosciuto"
                    : receipt.MerchantName,
                Date = receipt.PurchasedAt?.ToString("d MMM yyyy", Italian) ?? "Data mancante",
                Total = FormatCents(receipt.TotalCents),
                IsIncomplete = receipt.IsIncomplete,
            });
        }

        IsEmpty = Receipts.Count == 0;
        IncompleteCount = receipts.Count(r => r.IsIncomplete);
        BuildMonthSpend(receipts);
    }

    /// <summary>
    /// Spesa del mese corrente per esercente. Gli scontrini incompleti restano fuori: un
    /// totale che li includesse a zero mentirebbe sull'andamento della spesa.
    /// </summary>
    private void BuildMonthSpend(List<Receipt> receipts)
    {
        var now = DateTimeOffset.Now;
        MonthLabel = now.ToString("MMMM yyyy", Italian);

        var ofMonth = receipts
            .Where(r => !r.IsIncomplete)
            .Where(r => r.PurchasedAt!.Value.Year == now.Year && r.PurchasedAt!.Value.Month == now.Month)
            .ToList();

        MonthTotal = FormatCents(ofMonth.Sum(r => r.TotalCents!.Value));

        MonthByMerchant.Clear();
        var byMerchant = ofMonth
            .GroupBy(r => string.IsNullOrWhiteSpace(r.MerchantName)
                ? "Esercente non riconosciuto"
                : r.MerchantName!)
            .Select(g => new { Merchant = g.Key, Total = g.Sum(r => r.TotalCents!.Value) })
            .OrderByDescending(x => x.Total);

        foreach (var entry in byMerchant)
        {
            MonthByMerchant.Add(new MerchantSpendItem
            {
                Merchant = entry.Merchant,
                Amount = FormatCents(entry.Total),
            });
        }
    }

    /// <summary>Centesimi interi in importo leggibile; niente virgola mobile lungo il percorso.</summary>
    public static string FormatCents(long? cents) =>
        cents is null
            ? "—"
            : (cents.Value / 100m).ToString("C", Italian);
}
