using CardMaster.Services.Receipts;

namespace CardMaster.Tests;

/// <summary>
/// Quadratura: il confronto tra quello che l'app ha letto e quello che lo scontrino dichiara.
/// È la rete che rende dichiarabile l'incertezza invece che nascosta.
/// </summary>
public class ReceiptTotalsCheckTests
{
    private static ReceiptItemLine Line(long amount, int? rate = null) =>
        new("VOCE", "voce", ReceiptItemLine.SingleUnit, ReceiptItemUnit.Piece, null, amount, rate,
            amount < 0 ? ReceiptItemKind.Discount : ReceiptItemKind.Product, false, 0);

    [Fact]
    public void Somma_coincidente_quadra()
    {
        var balance = ReceiptTotalsCheck.Verify([Line(149), Line(119)], 268);

        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.Status);
        Assert.Equal(0, balance.DifferenceCents);
    }

    [Fact]
    public void Sconti_compresi_nella_somma()
    {
        var balance = ReceiptTotalsCheck.Verify([Line(199), Line(-100)], 99);

        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.Status);
    }

    [Fact]
    public void Scarto_dichiarato_non_corretto()
    {
        var balance = ReceiptTotalsCheck.Verify([Line(149), Line(119)], 300);

        Assert.Equal(ReceiptBalanceStatus.Mismatch, balance.Status);
        Assert.Equal(-32, balance.DifferenceCents);
        Assert.Equal(268, balance.LinesTotalCents);
    }

    /// <summary>
    /// Il caso pericoloso: la virgola spostata di un posto produce uno scarto piccolo, che a
    /// occhio si confonde con un arrotondamento. È esattamente ciò che la tolleranza zero serve
    /// a non lasciar passare.
    /// </summary>
    [Fact]
    public void Virgola_spostata_non_passa_per_arrotondamento()
    {
        var balance = ReceiptTotalsCheck.Verify([Line(150), Line(119)], 284);

        Assert.Equal(ReceiptBalanceStatus.Mismatch, balance.Status);
        Assert.Equal(-15, balance.DifferenceCents);
    }

    [Fact]
    public void Senza_totale_di_testata_le_righe_restano_non_validate()
    {
        var balance = ReceiptTotalsCheck.Verify([Line(149)], null);

        Assert.Equal(ReceiptBalanceStatus.NotChecked, balance.Status);
    }

    [Fact]
    public void Senza_riepilogo_la_quadratura_per_aliquota_non_si_fa()
    {
        var balance = ReceiptTotalsCheck.Verify([Line(149, 400)], 149);

        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.Status);
        Assert.Equal(ReceiptBalanceStatus.NotChecked, balance.RateStatus);
        Assert.Empty(balance.Rates);
    }

    /// <summary>
    /// Due errori di segno opposto lasciano corretto il totale complessivo: la somma torna, e
    /// senza il confronto per aliquota lo scontrino sembrerebbe verificato.
    /// </summary>
    [Fact]
    public void Errori_che_si_compensano_falliscono_per_aliquota()
    {
        var summary = new ReceiptVatSummary(
            [
                new ReceiptVatEntry("1", 400, 1000, 40),
                new ReceiptVatEntry("2", 1000, 1000, 100),
            ],
            140);

        var items = new[] { Line(1140, 400), Line(960, 1000) };

        var balance = ReceiptTotalsCheck.Verify(items, 2100, summary);

        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.Status);
        Assert.Equal(ReceiptBalanceStatus.Mismatch, balance.RateStatus);

        var unbalanced = balance.UnbalancedRates.ToList();
        Assert.Equal(2, unbalanced.Count);
        Assert.Equal(100, unbalanced[0].DifferenceCents);
        Assert.Equal(-140, unbalanced[1].DifferenceCents);
    }

    [Fact]
    public void Confronto_per_aliquota_sul_lordo_non_sull_imponibile()
    {
        var summary = new ReceiptVatSummary([new ReceiptVatEntry("1", 400, 1000, 40)], 40);

        var balance = ReceiptTotalsCheck.Verify([Line(1040, 400)], 1040, summary);

        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.RateStatus);
        Assert.Equal(1040, balance.Rates[0].DeclaredGrossCents);
    }

    [Fact]
    public void Righe_senza_aliquota_contate_non_attribuite()
    {
        var summary = new ReceiptVatSummary([new ReceiptVatEntry("1", 400, 1000, 40)], 40);

        var balance = ReceiptTotalsCheck.Verify([Line(1040, 400), Line(200)], 1240, summary);

        Assert.Equal(1, balance.LinesWithoutRate);
        Assert.Equal(ReceiptBalanceStatus.Balanced, balance.RateStatus);
        Assert.Equal(1040, balance.Rates[0].LinesCents);
    }
}
