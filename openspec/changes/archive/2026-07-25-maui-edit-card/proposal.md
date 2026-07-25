## Why

Oggi una carta, una volta salvata, non è più modificabile: se il nome è sbagliato, l'emittente non era stato indicato, o il formato è stato rilevato male in scansione, l'unica via è cancellare (funzione peraltro non ancora esposta) e ricrearla. Serve poter correggere una carta esistente e rimuoverla, mantenendo la semantica offline-first e tombstone già adottata.

## What Changes

- Nuova schermata di **modifica** di una carta esistente, raggiungibile dalla pagina di visualizzazione (toolbar "Modifica").
- Campi modificabili: **nome visualizzato**, **associazione emittente** (catalogo / libero / nessuno, con ri-arricchimento facoltativo di colore/logo/formato quando si sceglie dal catalogo) e **formato barcode** (correzione di un formato rilevato male).
- Il **valore del barcode resta immutabile**: mostrato in sola lettura (è l'identità della carta fedeltà). La decisione è motivata in design.md.
- Salvataggio via `ICardRepository.UpdateAsync`, che aggiorna `UpdatedAt` preservando `Id` e `CreatedAt` (compatibile con la futura sync).
- **Eliminazione** di una carta (soft-delete / tombstone) dalla pagina di visualizzazione, con conferma; usa `ICardRepository.SoftDeleteAsync` (oggi presente ma non esposto in UI). Dopo l'eliminazione l'utente torna alla lista e la carta non compare più.
- Nessuna nuova dipendenza: si riusano repository, catalogo emittenti e i pattern della schermata di conferma esistente.

## Capabilities

### New Capabilities
- `card-editing`: modifica dei campi editabili di una carta esistente (nome, emittente con ri-arricchimento, formato), con barcode immutabile, ed eliminazione logica (tombstone) con conferma; punto d'ingresso dalla pagina di visualizzazione carta.

### Modified Capabilities
<!-- Nessun cambiamento a requisiti di capability esistenti: l'avvio della modifica/eliminazione è
     descritto come requisito della nuova capability card-editing. -->

## Impact

- **Codice nuovo**: `EditCardViewModel`, `EditCardPage` (XAML + code-behind), rotta Shell `EditCardPage`, registrazione DI in `MauiProgram`.
- **Codice modificato**: `ShowCardPage` (toolbar "Modifica"/"Elimina" + ricarico dati al ritorno dalla modifica), `AppShell` (nuova rotta).
- **Repository**: nessuna modifica di interfaccia — si usano `UpdateAsync` e `SoftDeleteAsync` già presenti.
- **Dati**: nessuna migrazione di schema; l'aggiornamento tocca solo campi già esistenti su `Card`.
- **Build**: verifica finale `dotnet build` a 0 errori come criterio di accettazione.
