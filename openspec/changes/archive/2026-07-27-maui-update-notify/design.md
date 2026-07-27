## Context

`app-update` oggi espone solo un controllo **su richiesta esplicita** dalla pagina Impostazioni: nessun controllo automatico all'avvio o in background è ammesso dal requisito attuale. Questo protegge dal caso "l'app chiama rete senza che l'utente lo sappia", ma ha il costo che nessuno scopre gli aggiornamenti se non entra in Impostazioni. Vogliamo un segnale visibile prima di quel punto (es. lista carte), mantenendo il principio "nessuna rete senza che l'utente l'abbia esplicitamente permesso" spostandolo dal livello "singola richiesta" al livello "opzione abilitata una volta, poi l'app la esegue per conto suo entro limiti chiari" — lo stesso pattern già usato da `cloud-backup` per la schedulazione del backup automatico.

## Goals / Non-Goals

**Goals:**
- Segnalare la presenza di un aggiornamento disponibile in un punto visibile prima/fuori da Impostazioni (badge sull'icona Impostazioni; eventuale banner nella lista carte).
- Permettere il controllo automatico solo se l'utente lo abilita esplicitamente (opt-in, default off).
- Limitare la frequenza del controllo automatico (minimo 24h tra due controlli) e agganciarlo solo all'apertura/foreground dell'app — mai polling in background con l'app chiusa.
- Non silenziare per sempre l'avviso: se l'utente ignora il segnale, ricompare a ogni apertura finché non lo chiude esplicitamente o installa l'update; una volta chiuso, non ricompare per quella stessa versione remota.
- Riusare integralmente il client/servizio di controllo versione di `app-update` (stessa chiamata Release GitHub `latest`, stesso parsing) — questa change aggiunge solo *quando* scatta il controllo e *come* si segnala l'esito, non *come* si controlla.

**Non-Goals:**
- Non cambia il flusso di download/verifica checksum/installazione (resta `app-update`).
- Non introduce notifiche di sistema push né controllo mentre l'app è in background/chiusa (nessun background service, nessun WorkManager/AlarmManager).
- Non introduce un server/manifest aggiuntivo: la fonte resta la Release GitHub `latest` già usata.

## Decisions

- **Preferenza dedicata** `UpdateNotifyEnabled` (bool, default `false`) nello store `Preferences` esistente (stesso meccanismo di `app-settings`), esposta in Impostazioni come switch "Avvisami di nuove versioni". Scelta: coerente col pattern opt-in già usato per il backup Drive, invece di abilitare il controllo automatico di default (che romperebbe il vincolo "nessuna chiamata di rete non richiesta").
- **Innesco del controllo**: al passaggio dell'app in foreground (avvio o ripresa da background), se `UpdateNotifyEnabled = true` e sono trascorse almeno 24h dall'ultimo controllo (`UpdateNotifyLastCheckedAt` in Preferences). Alternativa scartata: timer/polling periodico indipendente dall'apertura app → richiederebbe un background service Android, complessità e consumo batteria non giustificati per un badge.
- **Persistenza dell'esito**: due nuove preference, `UpdateNotifyLastCheckedAt` (timestamp) e `UpdateNotifyAvailableVersion` (stringa, nome versione remota vista l'ultima volta, vuota se nessuna). Riusa lo stesso store già usato da `app-update` per "esito ultimo controllo" mostrato in Impostazioni — nessuna nuova tabella DB, solo preference key/value.
- **Silenziamento per versione**: `UpdateNotifyDismissedVersion` (stringa). Il segnale si mostra se `UpdateNotifyAvailableVersion` è non vuota, diversa dalla versione installata, e diversa da `UpdateNotifyDismissedVersion`. Chiudere il segnale imposta `UpdateNotifyDismissedVersion = UpdateNotifyAvailableVersion`. Se un controllo successivo trova una versione remota ulteriore (diversa da quella già silenziata), il segnale torna a comparire.
- **Superficie del segnale**: badge (pallino) sull'icona/voce "Impostazioni" nella toolbar/menu della lista carte — riusa un punto di navigazione già esistente, minimizza superficie UI nuova. Il banner nella lista carte resta opzionale/valutabile in fase di implementazione (task separato in `tasks.md`), non bloccante per il resto della change.
- **Nessuna chiamata di rete se l'opzione è disattivata**: la logica di innesco vive interamente lato client MAUI (nessun cambiamento server-side, non ce n'è uno). Se `UpdateNotifyEnabled = false`, il codice del controllo automatico non viene nemmeno invocato — stesso invariante già garantito oggi da `app-update`.

## Risks / Trade-offs

- [Rischio] L'utente non nota il badge sull'icona Impostazioni → Mitigazione: eventuale banner aggiuntivo nella lista carte (vedi sopra), valutabile senza bloccare il rilascio del solo badge.
- [Rischio] Controllo automatico percepito come "telemetria nascosta" nonostante sia opt-in → Mitigazione: copy chiaro nello switch delle Impostazioni ("interroga GitHub per verificare nuove versioni"), default OFF, stesso pattern già comunicato per il backup Drive.
- [Trade-off] Intervallo minimo fisso (24h) invece di configurabile → scelta di semplicità (come la frequenza di backup, che invece è scelta dall'utente); se in seguito serve configurabilità si aggiunge come estensione, non blocca questa change.

## Open Questions

- Banner nella lista carte: incluso in questa change o rimandato? (proposta: badge come requisito minimo, banner come task opzionale — da confermare in analisi).
- Copy esatto del segnale e dello switch in Impostazioni — da rifinire in fase di UI.
