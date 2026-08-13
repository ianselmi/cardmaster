using CardMaster.Services.Ai;
using CardMaster.Services.Receipts;

namespace CardMaster.Tests;

/// <summary>
/// Mappatura dell'esito del modello nelle strutture del dominio.
/// <para>
/// È il punto in cui un errore non fa rumore: un importo scalato di dieci o una data spostata di
/// un giorno non fanno crashare niente, spostano solo una quadratura o un totale mensile. E il
/// rifiuto delle risposte incomplete è ciò che impedisce di presentare mezzo scontrino come intero.
/// </para>
/// </summary>
public class ReceiptAiMapperTests
{
    private static readonly ReceiptAiUsage NoUsage = new(0, 0, "claude-opus-5");

    /// <summary>Riferimento fisso: l'offset locale della macchina non deve cambiare l'esito.</summary>
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 10, 0, 0, TimeSpan.FromHours(2));

    private const string ScontrinoCompleto = """
    {
      "merchant_name": "SUPERMERCATO MD",
      "merchant_vat_id": "01234567890",
      "purchased_at": "2026-08-11T18:42",
      "total_cents": 4774,
      "tax_cents": 312,
      "items": [
        {"description": "PASTA BARILLA 500", "quantity_milli": 1000, "unit": "piece",
         "unit_price_cents": null, "amount_cents": 149, "vat_rate_basis_points": 400, "kind": "product"},
        {"description": "BANANE", "quantity_milli": 432, "unit": "kilogram",
         "unit_price_cents": 199, "amount_cents": 86, "vat_rate_basis_points": 400, "kind": "product"},
        {"description": "SCONTO FIDELITY", "quantity_milli": 1000, "unit": "piece",
         "unit_price_cents": null, "amount_cents": -50, "vat_rate_basis_points": null, "kind": "discount"}
      ],
      "vat_summary": [
        {"code": "1", "rate_basis_points": 400, "taxable_cents": 2260, "tax_cents": 90},
        {"code": "2", "rate_basis_points": 2200, "taxable_cents": 1800, "tax_cents": 396}
      ]
    }
    """;

    [Fact]
    public void Risposta_conforme_produce_le_righe()
    {
        var result = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now);

        Assert.True(result.Succeeded);
        Assert.Equal(AiErrorKind.None, result.Error);
        Assert.Equal(3, result.Reading!.Items.Count);
    }

    [Fact]
    public void Importi_in_centesimi_interi()
    {
        var items = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now).Reading!.Items;

        Assert.Equal(149, items[0].AmountCents);
        Assert.Equal(86, items[1].AmountCents);
    }

    [Fact]
    public void Sconto_negativo_e_riconosciuto_come_tale()
    {
        var sconto = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now).Reading!.Items[2];

        Assert.Equal(-50, sconto.AmountCents);
        Assert.Equal(ReceiptItemKind.Discount, sconto.Kind);
    }

    [Fact]
    public void Quantita_in_millesimi_con_unita_di_misura()
    {
        var items = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now).Reading!.Items;

        Assert.Equal(ReceiptItemLine.SingleUnit, items[0].QuantityMilli);
        Assert.Equal(ReceiptItemUnit.Piece, items[0].Unit);

        // 0,432 kg: senza l'unità questo numero sarebbe indistinguibile da "0,432 pezzi".
        Assert.Equal(432, items[1].QuantityMilli);
        Assert.Equal(ReceiptItemUnit.Kilogram, items[1].Unit);
        Assert.Equal(199, items[1].UnitPriceCents);
    }

    [Fact]
    public void Aliquote_in_punti_base()
    {
        var reading = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now).Reading!;

        Assert.Equal(400, reading.Items[0].VatRateBasisPoints);
        Assert.Equal(2200, reading.VatSummary.Entries[1].RateBasisPoints);
    }

    [Fact]
    public void Aliquota_non_leggibile_resta_vuota()
    {
        var sconto = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now).Reading!.Items[2];

        // Mai dedotta e mai assunta per default: un'aliquota inventata è indistinguibile da una letta.
        Assert.Null(sconto.VatRateBasisPoints);
    }

    [Fact]
    public void Testata_letta_per_intero()
    {
        var header = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now).Reading!.Header;

        Assert.Equal("SUPERMERCATO MD", header.MerchantName);
        Assert.Equal("01234567890", header.MerchantVatId);
        Assert.Equal(4774, header.TotalCents);
        Assert.Equal(312, header.TaxCents);
    }

    [Fact]
    public void Data_letta_con_offset_locale_non_come_UTC()
    {
        var header = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now).Reading!.Header;

        // Il giorno stampato deve restare il giorno stampato: sqlite-net persiste DateTimeOffset
        // come tick UTC, e una data trattata come UTC tornerebbe indietro di un giorno,
        // spostando lo scontrino nel mese sbagliato a fine mese. Vedi docs/technical-notes.md.
        Assert.Equal(11, header.PurchasedAt!.Value.Day);
        Assert.Equal(18, header.PurchasedAt.Value.Hour);
        Assert.Equal(Now.Offset, header.PurchasedAt.Value.Offset);
    }

    [Fact]
    public void Le_righe_conservano_l_ordine_di_stampa()
    {
        var items = ReceiptAiMapper.Map(ScontrinoCompleto, NoUsage, Now).Reading!.Items;

        Assert.Equal([0, 1, 2], items.Select(i => i.Order));
    }

    [Fact]
    public void Il_consumo_effettivo_viene_riportato()
    {
        var usage = new ReceiptAiUsage(2000, 1200, "claude-opus-5");

        var reading = ReceiptAiMapper.Map(ScontrinoCompleto, usage, Now).Reading!;

        Assert.Equal(2000, reading.Usage.InputTokens);
        Assert.Equal(1200, reading.Usage.OutputTokens);
    }

    [Fact]
    public void Costo_effettivo_calcolato_dal_listino_del_modello()
    {
        var opus = ReceiptAiModels.Resolve("claude-opus-5");
        var usage = new ReceiptAiUsage(2000, 1200, opus.Id);

        // 2000 token in ingresso a $5/milione + 1200 in uscita a $25/milione = 4 centesimi circa.
        // In millesimi di centesimo: 2000*500000/1e6 + 1200*2500000/1e6 = 1000 + 3000 = 4000.
        Assert.Equal(4000, usage.CostMicroCents(opus));
    }

    // ---- Risposte che NON devono produrre righe ----

    [Fact]
    public void Risposta_troncata_e_un_errore_dichiarato()
    {
        // Il JSON si interrompe a metà: è il caso di una risposta tagliata dal limite di token.
        const string troncata = """
        {"merchant_name": "MD", "items": [{"description": "PASTA", "amount_cents": 1
        """;

        var result = ReceiptAiMapper.Map(troncata, NoUsage, Now);

        Assert.False(result.Succeeded);
        Assert.Equal(AiErrorKind.MalformedResponse, result.Error);
        Assert.Null(result.Reading);
    }

    [Fact]
    public void Una_sola_riga_inutilizzabile_fa_fallire_tutta_la_lettura()
    {
        // Prima riga buona, seconda senza importo. Mezzo scontrino presentato come intero
        // quadrerebbe per caso o sballerebbe senza spiegare perché: meglio nessuna riga.
        const string parziale = """
        {
          "merchant_name": null, "merchant_vat_id": null, "purchased_at": null,
          "total_cents": 300, "tax_cents": null,
          "items": [
            {"description": "PASTA", "quantity_milli": 1000, "unit": "piece",
             "unit_price_cents": null, "amount_cents": 149, "vat_rate_basis_points": null, "kind": "product"},
            {"description": "PANE", "quantity_milli": 1000, "unit": "piece",
             "unit_price_cents": null, "amount_cents": null, "vat_rate_basis_points": null, "kind": "product"}
          ],
          "vat_summary": []
        }
        """;

        var result = ReceiptAiMapper.Map(parziale, NoUsage, Now);

        Assert.False(result.Succeeded);
        Assert.Equal(AiErrorKind.MalformedResponse, result.Error);
        Assert.Null(result.Reading);
    }

    [Fact]
    public void Valore_fuori_dallo_schema_non_diventa_una_riga_inventata()
    {
        // "sconto" invece di "discount": non lo si interpreta a naso, si dichiara l'errore.
        const string fuoriSchema = """
        {
          "merchant_name": null, "merchant_vat_id": null, "purchased_at": null,
          "total_cents": null, "tax_cents": null,
          "items": [
            {"description": "PROMO", "quantity_milli": 1000, "unit": "piece",
             "unit_price_cents": null, "amount_cents": -50, "vat_rate_basis_points": null, "kind": "sconto"}
          ],
          "vat_summary": []
        }
        """;

        var result = ReceiptAiMapper.Map(fuoriSchema, NoUsage, Now);

        Assert.False(result.Succeeded);
        Assert.Null(result.Reading);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("non sono JSON")]
    [InlineData("{}")]
    public void Risposta_non_conforme_non_produce_letture(string? json)
    {
        var result = ReceiptAiMapper.Map(json, NoUsage, Now);

        Assert.False(result.Succeeded);
        Assert.Equal(AiErrorKind.MalformedResponse, result.Error);
    }

    [Fact]
    public void Data_illeggibile_svuota_il_campo_senza_perdere_le_righe()
    {
        const string dataStrana = """
        {
          "merchant_name": "MD", "merchant_vat_id": null, "purchased_at": "l'altro ieri",
          "total_cents": 149, "tax_cents": null,
          "items": [
            {"description": "PASTA", "quantity_milli": 1000, "unit": "piece",
             "unit_price_cents": null, "amount_cents": 149, "vat_rate_basis_points": null, "kind": "product"}
          ],
          "vat_summary": []
        }
        """;

        var result = ReceiptAiMapper.Map(dataStrana, NoUsage, Now);

        // Un campo illeggibile è un campo, non una risposta non conforme: si svuota e l'utente
        // lo corregge nella schermata di conferma, senza buttare via righe buone.
        Assert.True(result.Succeeded);
        Assert.Null(result.Reading!.Header.PurchasedAt);
        Assert.Single(result.Reading.Items);
    }
}
