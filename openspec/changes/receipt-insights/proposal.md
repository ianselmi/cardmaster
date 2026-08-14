## Why

Gli scontrini oggi si accumulano ma non rispondono a nessuna domanda. L'app sa dire quanto si è speso **questo mese** e **da chi**, e nient'altro: cosa si compra più spesso, quali categorie pesano davvero, se il caffè costa più di sei mesi fa sono tutte informazioni già nel database — righe, categorie, aliquote, date — che nessuna schermata legge. È la ragione per cui le tre change precedenti sono state fatte: senza le viste, `receipt-items` e `receipt-categories` sono lavoro pagato e mai riscosso.

C'è anche una ragione di verifica, non solo di prodotto. La ricostruzione delle righe è ferma a **24 su 29** su scontrino reale, e finora l'unico giudizio su quel numero è aritmetico (la quadratura). Le classifiche dicono un'altra cosa, che la quadratura non può dire: se quelle righe, coi loro nomi grezzi e le loro imperfezioni, **producono classifiche leggibili da un essere umano**. È la misura che serve prima di decidere quanto vale `receipt-ai-normalize`.

## What Changes

- Nuova sezione **Analisi**, raggiungibile dalla sezione Scontrini, con quattro viste:
  - **Top prodotti** — per numero di acquisti e per spesa totale, sulla descrizione normalizzata già persistita su ogni riga.
  - **Top categorie** — spesa per categoria, con la quota rimasta **senza categoria** dichiarata invece che nascosta.
  - **Spesa per negozio e per mese** — l'aggregazione che oggi esiste solo per il mese corrente, estesa a tutto lo storico.
  - **Andamento del prezzo di un prodotto** — serie storica del prezzo unitario, con sparkline SkiaSharp (già in casa per i barcode).
- Le aggregazioni si calcolano in **SQL**, non caricando lo storico in memoria. Oggi la spesa per esercente del mese è un `GetAllAsync()` seguito da LINQ: accettabile su un mese, insostenibile su un `GROUP BY` di tutte le righe di tutti gli scontrini.
- Ogni vista dichiara **su quanti dati si basa** e quanta parte dello storico non ha potuto usare (scontrini senza righe, righe senza categoria, righe senza prezzo unitario). Una classifica costruita su metà dei dati e presentata come completa è peggio di nessuna classifica.
- Le viste **non correggono e non nascondono**: nessuna riga viene esclusa perché "sembra sbagliata", nessun totale viene aggiustato per farlo tornare.
- Nessuna modifica allo schema del database: tutto ciò che serve è già in `ReceiptItem` (descrizione normalizzata, categoria, prezzo unitario, unità, importo) e in `Receipt` (esercente, data). Nessuna rete, nessuna dipendenza nuova, nessun permesso nuovo, nessun segreto.

## Capabilities

### New Capabilities
- `receipt-insights`: viste di analisi sulla spesa — top prodotti, top categorie, spesa per negozio e mese, andamento del prezzo di un prodotto — calcolate in SQL sullo storico locale, ciascuna con la dichiarazione esplicita della copertura dei dati su cui si basa.

### Modified Capabilities
- `local-storage`: le letture di analisi sono aggregazioni SQL sullo storico, non caricamenti in memoria; servono gli indici che le rendono sostenibili al crescere degli scontrini.
- `receipt-scan`: la spesa per esercente e per mese, oggi limitata al mese corrente e calcolata in memoria nella lista scontrini, diventa una delle viste di analisi ed è consultabile su tutto lo storico.

## Impact

- **Nuovo**: `Services/Receipts/IReceiptAnalytics` e implementazione SQL; ViewModel e pagine delle quattro viste; controllo sparkline su SkiaSharp.
- **Modificato**: `ReceiptListViewModel` (la spesa del mese smette di essere calcolata a mano lì); `AppShell` per l'accesso alla sezione; `DatabaseService` per gli indici di aggregazione.
- **Test**: le query SQL non sono logica pura e non entrano nel progetto di test attuale, che compila i sorgenti dell'app senza database — il confine dichiarato in `CLAUDE.md`. Resta testabile la logica pura di formattazione e di calcolo della copertura; ciò che dipende dal database si verifica su emulatore con dati reali. Se servisse di più, la strada dichiarata è estrarre una libreria condivisa, non allungare la lista dei file collegati.
- **Non tocca**: rete (nessuna), dipendenze (nessuna nuova), permessi (nessuno), schema del database (invariato), credenziali (nessuna).
