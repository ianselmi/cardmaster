using CardMaster.Services.Receipts;

namespace CardMaster.Tests;

/// <summary>
/// Estrazione dei dati di testata dal testo di uno scontrino.
/// <para>
/// I casi con l'etichetta «OCR reale» vengono da output vero di ML Kit su emulatore
/// (11 ago 2026) e non sarebbero mai stati scritti a tavolino: sono quelli che hanno
/// fatto emergere i difetti veri.
/// </para>
/// </summary>
public class ReceiptHeaderParserTests
{
    /// <summary>Istante di riferimento fisso: la plausibilità delle date non deve dipendere da oggi.</summary>
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 12, 0, 0, TimeSpan.FromHours(2));

    private static ReceiptHeader Parse(string text) => ReceiptHeaderParser.Parse(text, Now);

    [Fact]
    public void Il_totale_vince_su_subtotale_sconti_e_resto()
    {
        var header = Parse("""
            ESSELUNGA S.P.A.
            P.IVA 04916380159
            SUBTOTALE                  7,11
            SCONTO FIDATY             -0,50
            TOTALE COMPLESSIVO         6,61
            Pagamento contante        10,00
            RESTO                      3,39
            11/08/2026 18:42
            """);

        Assert.Equal("ESSELUNGA S.P.A.", header.MerchantName);
        Assert.Equal("04916380159", header.MerchantVatId);
        Assert.Equal(661, header.TotalCents);
        Assert.Equal(new DateTimeOffset(2026, 8, 11, 18, 42, 0, Now.Offset), header.PurchasedAt);
    }

    [Fact]
    public void Importo_sulla_riga_successiva_alla_parola_chiave()
    {
        var header = Parse("""
            LIDL ITALIA SRL
            Partita IVA 13032450150
            TOT. EURO
            3,92
            10-08-2026 09:15
            """);

        Assert.Equal(392, header.TotalCents);
    }

    [Fact]
    public void Importo_oltre_mille_col_punto_delle_migliaia()
    {
        var header = Parse("""
            COOP ALLEANZA 3.0
            P.IVA 02201090368
            TOTALE                1.234,56
            05.08.2026 20:03
            """);

        Assert.Equal(123456, header.TotalCents);
    }

    [Fact]
    public void Ora_letta_solo_con_i_due_punti_non_dalla_data_puntata()
    {
        // Regressione: con il punto ammesso come separatore dell'ora, "05.08.2026"
        // veniva letto come le 05:08. Un'ora sbagliata è peggio di un'ora mancante.
        var header = Parse("""
            COOP ALLEANZA 3.0
            TOTALE                    9,90
            05.08.2026 20:03
            """);

        Assert.Equal(new DateTimeOffset(2026, 8, 5, 20, 3, 0, Now.Offset), header.PurchasedAt);
    }

    [Fact]
    public void Data_futura_scartata_invece_di_essere_accettata()
    {
        var header = Parse("""
            CONAD CITY
            P.IVA 01234567890
            TOTALE                   12,30
            31/12/2027 10:00
            """);

        Assert.Null(header.PurchasedAt);
        Assert.Equal(1230, header.TotalCents);
    }

    [Fact]
    public void Senza_riga_di_totale_il_campo_resta_vuoto()
    {
        // Lo scontrino resta comunque salvabile: la correzione a mano è un percorso
        // di prima classe, non un ripiego. Se un domani il totale diventasse
        // obbligatorio, questo test segnala che è cambiata una decisione di prodotto.
        var header = Parse("""
            BAR CENTRALE
            P.IVA 09876543210
            CAFFE                     1,20
            CORNETTO                  1,50
            11/08/2026
            """);

        Assert.Null(header.TotalCents);
        Assert.Equal("BAR CENTRALE", header.MerchantName);
    }

    [Fact]
    public void Testo_vuoto_non_lancia_e_non_inventa_nulla()
    {
        var header = Parse("   ");

        Assert.Null(header.MerchantName);
        Assert.Null(header.MerchantVatId);
        Assert.Null(header.PurchasedAt);
        Assert.Null(header.TotalCents);
    }

    [Fact]
    public void Importo_pagato_riconosciuto_come_totale()
    {
        var header = Parse("""
            FARMACIA SAN MARCO
            CORSO ITALIA 44
            P.IVA 11122233344
            TACHIPIRINA               6,50
            IMPORTO PAGATO            6,50
            11/08/2026 11:05
            """);

        Assert.Equal(650, header.TotalCents);
        Assert.Equal("FARMACIA SAN MARCO", header.MerchantName);
    }

    [Fact]
    public void Insegna_trovata_anche_se_non_e_la_prima_riga()
    {
        var header = Parse("""
            *** 123 ***
            PANIFICIO DA MARIO
            VIA ROMA 12
            P.IVA 55566677788
            TOTALE                    8,00
            09/08/2026
            """);

        Assert.Equal("PANIFICIO DA MARIO", header.MerchantName);
    }

    [Fact]
    public void OCR_reale_data_spezzata_dopo_la_barra()
    {
        // Il riconoscimento restituisce "11/08/ 2026": senza tolleranza agli spazi
        // attorno ai separatori la data veniva scartata pur essendo leggibile.
        var header = Parse("""
            ESSELUNGA S.P.A.
            P.IVA 04916380159
            TOTALE COMPLESSIVO  6,61
            11/08/ 2026 18:42
            """);

        Assert.Equal(new DateTimeOffset(2026, 8, 11, 18, 42, 0, Now.Offset), header.PurchasedAt);
    }

    [Fact]
    public void OCR_reale_spazio_dentro_un_importo_negativo()
    {
        // "-0, 50" sulla riga dello sconto non deve confondere la ricerca del totale.
        var header = Parse("""
            ESSELUNGA S.P.A.
            SUBTOTALE  7,11
            SCONTO FIDATY  -0, 50
            TOTALE COMPLESSIVO  6,61
            11/08/ 2026 18:42
            """);

        Assert.Equal(661, header.TotalCents);
    }
}
