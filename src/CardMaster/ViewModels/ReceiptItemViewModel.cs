using System.Globalization;
using CardMaster.Data;
using CardMaster.Services;
using CardMaster.Services.Receipts;

namespace CardMaster.ViewModels;

/// <summary>
/// Una riga dello scontrino nella schermata di conferma, <b>modificabile a mano</b>.
/// <para>
/// I campi sono testo e non numeri: l'utente digita, e ciò che digita va riletto con le stesse
/// tolleranze del resto dell'app (virgola o punto come separatore). Un campo numerico che
/// rifiuta l'input a metà digitazione è il modo più veloce per far abbandonare la correzione.
/// </para>
/// </summary>
public sealed class ReceiptItemViewModel : ObservableObject
{
    private string _description = string.Empty;
    private string _quantityText = "1";
    private string _amountText = string.Empty;
    private string _vatText = string.Empty;
    private string? _category;

    /// <summary>Notifica al form che questa riga è cambiata, per ricalcolare la quadratura.</summary>
    public event EventHandler? Changed;

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                Raise();
            }
        }
    }

    /// <summary>Quantità in pezzi o chili, come la scrive l'utente.</summary>
    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (SetProperty(ref _quantityText, value))
            {
                Raise();
            }
        }
    }

    public string AmountText
    {
        get => _amountText;
        set
        {
            if (SetProperty(ref _amountText, value))
            {
                Raise();
            }
        }
    }

    /// <summary>Aliquota in percentuale; vuota quando non è stata letta, e resta vuota.</summary>
    public string VatText
    {
        get => _vatText;
        set
        {
            if (SetProperty(ref _vatText, value))
            {
                Raise();
            }
        }
    }

    /// <summary>Categoria assegnata, <c>null</c> se nessuna sorgente l'ha riconosciuta.</summary>
    public string? Category
    {
        get => _category;
        set
        {
            if (SetProperty(ref _category, value))
            {
                CategoryChangedByUser = true;
                Raise();
            }
        }
    }

    /// <summary>
    /// Vero quando la categoria è stata scelta dall'utente in questa sessione: solo queste
    /// diventano mappature apprese, perché una classificazione automatica confermata per inerzia
    /// non è una scelta.
    /// </summary>
    public bool CategoryChangedByUser { get; private set; }

    /// <summary>Sconto invece che prodotto.</summary>
    public bool IsDiscount { get; set; }

    /// <summary>Quantità per prezzo unitario che non torna con il totale di riga stampato.</summary>
    public bool IsInconsistent { get; set; }

    public string? UnitDisplay { get; set; }

    public bool HasWarning => IsInconsistent;

    private long? UnitPriceCents { get; set; }

    private ReceiptItemUnit Unit { get; set; } = ReceiptItemUnit.Piece;

    /// <summary>Costruisce la riga modificabile da una riga ricostruita dal parser.</summary>
    public static ReceiptItemViewModel FromLine(ReceiptItemLine line, string? category) => new()
    {
        _description = line.RawDescription,
        _quantityText = FormatQuantity(line.QuantityMilli, line.Unit),
        _amountText = FormatCents(line.AmountCents),
        _vatText = FormatRate(line.VatRateBasisPoints),
        _category = category,
        IsDiscount = line.Kind == ReceiptItemKind.Discount,
        IsInconsistent = line.IsInconsistent,
        UnitPriceCents = line.UnitPriceCents,
        Unit = line.Unit,
        UnitDisplay = line.Unit == ReceiptItemUnit.Kilogram ? "kg" : "pz",
    };

    /// <summary>Costruisce la riga modificabile da una riga già salvata.</summary>
    public static ReceiptItemViewModel FromEntity(ReceiptItem item) => new()
    {
        _description = item.Description,
        _quantityText = FormatQuantity(item.QuantityMilli, item.Unit),
        _amountText = FormatCents(item.AmountCents),
        _vatText = FormatRate(item.VatRateBasisPoints),
        _category = item.Category,
        IsDiscount = item.Kind == ReceiptItemKind.Discount,
        IsInconsistent = item.IsInconsistent,
        UnitPriceCents = item.UnitPriceCents,
        Unit = item.Unit,
        UnitDisplay = item.Unit == ReceiptItemUnit.Kilogram ? "kg" : "pz",
    };

    /// <summary>Riga vuota, per quando il riconoscimento ne ha persa una.</summary>
    public static ReceiptItemViewModel Empty() => new();

    /// <summary>
    /// Imposta la categoria <b>senza</b> considerarla una scelta dell'utente. Serve a mostrare
    /// il nome di una categoria già salvata: rileggerla dal database non è una correzione, e
    /// trattarla come tale creerebbe mappature apprese che l'utente non ha mai chiesto.
    /// </summary>
    public void SetCategoryQuietly(string? category)
    {
        if (SetProperty(ref _category, category, nameof(Category)))
        {
            CategoryChangedByUser = false;
        }
    }

    /// <summary>Importo della riga in centesimi; zero se il testo non è leggibile come importo.</summary>
    public long AmountCents
    {
        get
        {
            var value = ParseCents(AmountText) ?? 0;
            return IsDiscount && value > 0 ? -value : value;
        }
    }

    /// <summary>Aliquota in punti base, <c>null</c> se il campo è vuoto o illeggibile.</summary>
    public int? VatRateBasisPoints
    {
        get
        {
            var value = ParseCents(VatText);
            return value is null or < 0 ? null : (int)value.Value;
        }
    }

    /// <summary>
    /// Vista della riga come la vede la quadratura. Ricalcolata dal testo corrente, così la
    /// verifica guarda ciò che l'utente sta scrivendo e non ciò che il parser aveva letto.
    /// </summary>
    public ReceiptItemLine ToLine() => new(
        Description,
        TextNormalizer.Normalize(Description),
        ParseQuantityMilli(QuantityText),
        Unit,
        UnitPriceCents,
        AmountCents,
        VatRateBasisPoints,
        IsDiscount ? ReceiptItemKind.Discount : ReceiptItemKind.Product,
        IsInconsistent,
        0);

    /// <summary>Trasforma la riga modificata nell'entità da persistere.</summary>
    public ReceiptItem ToEntity(string receiptId) => new()
    {
        ReceiptId = receiptId,
        Description = Description.Trim(),
        NormalizedDescription = TextNormalizer.Normalize(Description),
        QuantityMilli = ParseQuantityMilli(QuantityText),
        Unit = Unit,
        UnitPriceCents = UnitPriceCents,
        AmountCents = AmountCents,
        VatRateBasisPoints = VatRateBasisPoints,
        Kind = IsDiscount ? ReceiptItemKind.Discount : ReceiptItemKind.Product,
        Category = string.IsNullOrWhiteSpace(Category) ? null : Category,
        IsInconsistent = IsInconsistent,
    };

    private void Raise() => Changed?.Invoke(this, EventArgs.Empty);

    private static string FormatCents(long cents) =>
        (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string FormatRate(int? basisPoints) =>
        basisPoints is null
            ? string.Empty
            : (basisPoints.Value / 100m).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string FormatQuantity(long milli, ReceiptItemUnit unit) =>
        unit == ReceiptItemUnit.Kilogram
            ? (milli / 1000m).ToString("0.###", CultureInfo.InvariantCulture).Replace('.', ',')
            : (milli / 1000m).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    private static long ParseQuantityMilli(string text)
    {
        var value = ParseDecimal(text);
        if (value is null or <= 0)
        {
            return ReceiptItemLine.SingleUnit;
        }

        return (long)Math.Round(value.Value * 1000m, MidpointRounding.AwayFromZero);
    }

    private static long? ParseCents(string? text)
    {
        var value = ParseDecimal(text);
        return value is null ? null : (long)Math.Round(value.Value * 100m, MidpointRounding.AwayFromZero);
    }

    private static decimal? ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
