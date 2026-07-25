## Why

Nella pagina Impostazioni, il pulsante "Backup su Google Drive" ha oggi sempre lo stesso aspetto, indipendentemente dal fatto che il backup sia attivo o meno. Per sapere se è attivo l'utente deve entrare nella sezione dedicata (`BackupPage`). Un segnale visivo diretto nel pulsante evita quel passaggio per il caso più comune ("è attivo il mio backup?").

## What Changes

- Il pulsante "Backup su Google Drive" nella pagina Impostazioni mostra un segnale visivo (sottotitolo di stato e/o colore diverso) quando il backup è attivo, riflettendo lo stato già persistito (`ISettingsStore.BackupEnabled`).
- Nessuna modifica al comportamento di abilitazione/disabilitazione/backup/ripristino, che restano interamente nella sezione dedicata (`BackupPage`, capability `cloud-backup`).
- Lo stato si aggiorna leggendo la preferenza corrente ogni volta che la pagina Impostazioni viene mostrata (coerente con l'attuale ricreazione della pagina/ViewModel a ogni navigazione).

## Capabilities

### Modified Capabilities
- `app-settings`: il requisito "Sezione Backup su Google Drive nelle Impostazioni" si arricchisce di un segnale visivo di stato (attivo/non attivo) mostrato direttamente sul pulsante che apre la sezione, oltre all'apertura della sezione stessa.

## Impact

- `SettingsViewModel`/`SettingsPage.xaml`: nuova proprietà di stato (es. testo sottotitolo, colore) letta da `ISettingsStore.BackupEnabled`; nessuna nuova dipendenza, nessuna chiamata di rete aggiuntiva.
- Nessun impatto su `cloud-backup`, `BackupPage`/`BackupViewModel`, backend o v2.
