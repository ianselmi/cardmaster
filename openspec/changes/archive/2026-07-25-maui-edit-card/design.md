## Context

Le carte sono persistite localmente (SQLite in chiaro) con `Id` client-generato e semantica tombstone. Il repository `ICardRepository` espone già `UpdateAsync` e `SoftDeleteAsync`, ma nessuna UI li usa: oggi si può solo creare (`AddCardPage` / `AddCardViewModel`) e visualizzare (`ShowCardPage` / `ShowCardViewModel`) una carta. Il flusso di creazione include già la logica di selezione emittente con arricchimento dal catalogo (`IIssuerCatalog`). Questa change espone modifica ed eliminazione riusando quei pattern, senza toccare l'interfaccia del repository né lo schema dati.

## Goals / Non-Goals

**Goals:**
- Correggere una carta esistente: nome, associazione emittente (con ri-arricchimento), formato barcode.
- Eliminare una carta con conferma, in modo logico (tombstone).
- Riusare i pattern esistenti (selezione emittente/arricchimento, validazione, navigazione Shell) per coerenza e minimo codice nuovo.
- Rispettare la compatibilità con la futura sync: preservare `Id`/`CreatedAt`, rinnovare `UpdatedAt`.

**Non-Goals:**
- Modificare il **valore** del barcode (resta immutabile: è l'identità della carta fedeltà).
- Controllo duplicati sul barcode in modifica (il valore non cambia, quindi non serve).
- Undo dell'eliminazione o cestino (il tombstone abilita un eventuale ripristino futuro, ma non è esposto ora).
- Modifica in blocco / multi-selezione dalla lista.

## Decisions

**Schermata dedicata `EditCardPage` + `EditCardViewModel` (non riuso diretto di AddCard).**
`AddCardViewModel` è modellato attorno alla creazione (`IQueryAttributable` su barcode/format, `AddAsync`, avviso duplicati, campo barcode editabile). Riutilizzarlo per la modifica lo appesantirebbe di rami condizionali (modalità create vs edit). Si preferisce un ViewModel separato che condivide la stessa logica di selezione emittente/arricchimento ma carica per `id`, mostra il barcode in sola lettura e salva con `UpdateAsync`. *Alternativa scartata:* aggiungere una modalità a `AddCardViewModel` — più rischio di regressioni sul flusso di creazione, già coperto da spec.

**Barcode immutabile, mostrato read-only.**
Coerente con il principio "il barcode fedeltà è immutabile" del prodotto. Evita anche la necessità di ri-validare duplicati in modifica. *Alternativa scartata:* consentire la modifica del valore — introdurrebbe controllo duplicati, casi di collisione e ambiguità con la condivisione; non giustificata per una correzione di metadati.

**Ingresso da `ShowCardPage` via ToolbarItem ("Modifica", "Elimina").**
La carta è già aperta e in contesto; è il punto naturale per correggere/eliminare. Al ritorno dalla modifica, `ShowCardPage` ricarica i dati (il `ShowCardViewModel` ha un guard `_loaded` che va reso ri-eseguibile o bypassato al re-appear) così la visualizzazione riflette i nuovi valori. *Alternativa scartata:* swipe-to-delete dalla lista — utile ma è un'interazione a parte; l'eliminazione dalla pagina della carta, con conferma, copre il bisogno con meno superficie UI.

**Persistenza via `ICardRepository.UpdateAsync` / `SoftDeleteAsync` (già esistenti).**
`UpdateAsync` rinnova `UpdatedAt` e preserva il resto; `SoftDeleteAsync` imposta `DeletedAt` con UPDATE (mai DELETE fisico) e le query attive escludono i tombstone. Nessuna modifica di interfaccia o schema.

**Conferma eliminazione con `DisplayAlert`.**
Azione distruttiva → conferma esplicita nativa (Elimina/Annulla), semplice e coerente con MAUI.

## Risks / Trade-offs

- **Re-load della ShowCardPage al ritorno dalla modifica** → il guard `_loaded` in `ShowCardViewModel` impedirebbe l'aggiornamento; mitigazione: forzare un reload in `OnAppearing`/`OnNavigatedTo` (reset del flag o metodo `ReloadAsync`).
- **Ri-arricchimento che sovrascrive personalizzazioni** → scegliere un emittente dal catalogo può reimpostare colore/logo/formato; mitigazione: applicare l'arricchimento solo sui campi coerenti (come in creazione, dove il nome non viene sovrascritto se già presente) e documentare che il colore/logo derivano dall'emittente.
- **Eliminazione irreversibile dal punto di vista UI** → nessun undo esposto; mitigazione: conferma esplicita; il tombstone consente un futuro ripristino se necessario.
- **Coerenza formato ↔ valore** → cambiare formato può rendere il valore non renderizzabile come barcode; già gestito a valle da card-display ("barcode non generabile" mostra il codice in chiaro), nessun crash.

## Migration Plan

Nessuna migrazione dati: si aggiornano solo campi esistenti su `Card`. Deploy come normale build dell'app. Rollback = rimozione della rotta/pagina; i dati restano invariati. Criterio di accettazione finale: `dotnet build` a 0 errori.
