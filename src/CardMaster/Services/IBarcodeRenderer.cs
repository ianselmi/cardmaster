namespace CardMaster.Services;

/// <summary>Esito del rendering di un barcode.</summary>
/// <param name="Image">Immagine del barcode (null se non generabile).</param>
/// <param name="Succeeded">Vero se il barcode è stato generato.</param>
public readonly record struct BarcodeRenderResult(ImageSource? Image, bool Succeeded)
{
    public static BarcodeRenderResult Fail() => new(null, false);
    public static BarcodeRenderResult Ok(ImageSource image) => new(image, true);
}

/// <summary>
/// Genera l'immagine di un barcode dal valore e dal formato. Non lancia mai:
/// se il valore non è generabile nel formato indicato restituisce un esito fallito.
/// </summary>
public interface IBarcodeRenderer
{
    BarcodeRenderResult Render(string value, string format);
}
