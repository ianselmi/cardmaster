using BarcodeScanning;

namespace CardMaster.Data;

/// <summary>
/// Formati barcode supportati da CardMaster e mappatura verso/da l'enum della
/// libreria di scansione (ML Kit). I valori stringa sono stabili e persistiti
/// nel campo <see cref="Card.BarcodeFormat"/>.
/// </summary>
public static class BarcodeFormatCatalog
{
    // Valori stringa stabili usati in persistenza e nel picker manuale.
    public const string Ean13 = "EAN13";
    public const string Ean8 = "EAN8";
    public const string Code128 = "CODE128";
    public const string Code39 = "CODE39";
    public const string Itf = "ITF";
    public const string Codabar = "CODABAR";
    public const string QrCode = "QR_CODE";
    public const string Pdf417 = "PDF417";
    public const string UpcA = "UPCA";
    public const string UpcE = "UPCE";

    /// <summary>Elenco dei formati supportati (per il picker dell'inserimento manuale).</summary>
    public static IReadOnlyList<string> Supported { get; } = new[]
    {
        Ean13, Ean8, UpcA, UpcE, Code128, Code39, Itf, Codabar, QrCode, Pdf417,
    };

    /// <summary>Simbologie da abilitare nello scanner (unione dei formati supportati).</summary>
    public static BarcodeFormats ScannerSymbologies =>
        BarcodeFormats.Ean13 | BarcodeFormats.Ean8 | BarcodeFormats.Upca |
        BarcodeFormats.Upce | BarcodeFormats.Code128 | BarcodeFormats.Code39 |
        BarcodeFormats.Itf | BarcodeFormats.CodaBar | BarcodeFormats.QRCode |
        BarcodeFormats.Pdf417;

    /// <summary>
    /// Converte il formato rilevato dalla libreria nella stringa stabile di CardMaster,
    /// oppure null se il formato non è tra quelli supportati.
    /// </summary>
    public static string? FromScanner(BarcodeFormats format) => format switch
    {
        BarcodeFormats.Ean13 => Ean13,
        BarcodeFormats.Ean8 => Ean8,
        BarcodeFormats.Upca => UpcA,
        BarcodeFormats.Upce => UpcE,
        BarcodeFormats.Code128 => Code128,
        BarcodeFormats.Code39 => Code39,
        BarcodeFormats.Itf => Itf,
        BarcodeFormats.CodaBar => Codabar,
        BarcodeFormats.QRCode => QrCode,
        BarcodeFormats.Pdf417 => Pdf417,
        _ => null,
    };

    /// <summary>
    /// Converte la stringa di formato nel corrispondente enum di ZXing (per il rendering),
    /// oppure null se non mappabile.
    /// </summary>
    public static ZXing.BarcodeFormat? ToZXing(string? format) => format switch
    {
        Ean13 => ZXing.BarcodeFormat.EAN_13,
        Ean8 => ZXing.BarcodeFormat.EAN_8,
        UpcA => ZXing.BarcodeFormat.UPC_A,
        UpcE => ZXing.BarcodeFormat.UPC_E,
        Code128 => ZXing.BarcodeFormat.CODE_128,
        Code39 => ZXing.BarcodeFormat.CODE_39,
        Itf => ZXing.BarcodeFormat.ITF,
        Codabar => ZXing.BarcodeFormat.CODABAR,
        QrCode => ZXing.BarcodeFormat.QR_CODE,
        Pdf417 => ZXing.BarcodeFormat.PDF_417,
        _ => null,
    };

    /// <summary>Vero per i formati bidimensionali (QR, PDF417).</summary>
    public static bool Is2D(string? format) => format is QrCode or Pdf417;
}
