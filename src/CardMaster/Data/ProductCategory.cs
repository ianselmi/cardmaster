using System.Text.Json.Serialization;

namespace CardMaster.Data;

/// <summary>
/// Categoria di spesa del dizionario seed, bundle nell'app come il catalogo emittenti.
/// <para>
/// Le categorie sono <b>poche e larghe</b> per scelta: molte e fini classificherebbero meglio i
/// casi facili e sbaglierebbero di più su tutto il resto, rendendo illeggibili le viste di
/// analisi. Quello che il seed non copre lo copre l'apprendimento locale, che si adatta a chi
/// usa l'app invece di indovinare in anticipo cosa compra.
/// </para>
/// </summary>
public sealed class ProductCategory
{
    /// <summary>Identificativo stabile, conservato sulle righe e nelle mappature.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Nome mostrato all'utente.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Parole chiave che fanno ricadere una descrizione in questa categoria.</summary>
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = [];
}
