using SQLite;

namespace CardMaster.Data;

/// <summary>
/// Scontrino acquisito da foto o immagine, locale al device. In questa change (receipt-capture)
/// contiene i soli dati di <b>testata</b>: le righe dei prodotti arrivano con receipt-items.
/// </summary>
public class Receipt : EntityBase
{
    /// <summary>Nome dell'esercente letto in cima allo scontrino, se riconosciuto.</summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// Partita IVA dell'esercente (11 cifre), se riconosciuta. Più stabile del nome per
    /// raggruppare lo stesso negozio: l'intestazione cambia grafia tra uno scontrino e l'altro.
    /// </summary>
    public string? MerchantVatId { get; set; }

    /// <summary>Data e ora dell'acquisto, se riconosciute o inserite dall'utente.</summary>
    public DateTimeOffset? PurchasedAt { get; set; }

    /// <summary>
    /// Totale dello scontrino in <b>centesimi</b>. Intero e mai virgola mobile: le somme
    /// della vista di spesa devono essere esatte al centesimo.
    /// </summary>
    public long? TotalCents { get; set; }

    /// <summary>Valuta dell'importo. Oggi sempre "EUR"; il campo esiste per non chiudere porte.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Testo riconosciuto integrale. Conservato apposta: permette di ri-estrarre i dati di
    /// testata quando le regole miglioreranno, senza chiedere all'utente di rifotografare
    /// scontrini che non ha più.
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Percorso dell'immagine <b>relativo</b> alla cartella dati dell'app; null se l'utente
    /// ha scelto di non conservarla o se l'ha eliminata per liberare spazio.
    /// <para>
    /// L'immagine sta su filesystem e non nel database: un BLOB per scontrino gonfierebbe
    /// ogni snapshot caricato su Drive. Conseguenza dichiarata all'utente: le immagini
    /// <b>non</b> sono comprese nel backup e non tornano dopo un ripristino.
    /// </para>
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Vero se mancano data o totale: lo scontrino resta consultabile ma è escluso dai
    /// totali di spesa, e va segnalato all'utente perché lo completi.
    /// </summary>
    [Ignore]
    public bool IsIncomplete => PurchasedAt is null || TotalCents is null;
}
