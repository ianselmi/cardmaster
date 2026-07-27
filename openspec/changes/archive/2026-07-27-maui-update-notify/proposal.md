## Why

Oggi (`app-update`) l'utente scopre un aggiornamento disponibile solo entrando in Impostazioni e avviando un controllo manuale. Chi non ci pensa resta su versioni vecchie anche a lungo. Serve un segnale visibile altrove nell'app (es. lista carte) che avvisi "c'è un aggiornamento" senza dover andare a cercarlo, mantenendo comunque l'utente in controllo (nessuna sorpresa di rete non voluta).

## What Changes

- Nuovo segnale in-app (es. badge sull'icona Impostazioni e/o banner discreto nella lista carte) che indica la presenza di un aggiornamento disponibile, visibile fuori dalla pagina Impostazioni.
- Per alimentare il segnale, il controllo aggiornamenti può ora scattare anche **senza un'azione esplicita per-singola-richiesta** dell'utente, ma solo se l'utente ha **attivato l'opzione** (opt-in, default off) in Impostazioni "Avvisami di nuove versioni"; se disattivata il comportamento resta quello attuale (controllo solo su richiesta manuale).
- Il controllo automatico, quando abilitato, avviene al massimo una volta ogni intervallo minimo (es. non più di 1 volta ogni 24h) e solo quando l'app viene aperta — mai in background/silenzioso mentre l'app è chiusa.
- Il segnale è **silenziabile per versione**: se l'utente lo chiude/ignora, non ricompare per la stessa versione remota (ricompare se ne esce una più recente).
- Toccare il segnale porta l'utente al flusso di aggiornamento già esistente (`app-update`: download, verifica, installazione) — nessuna duplicazione di quella logica.

## Capabilities

### New Capabilities
- `app-update-notify`: segnale in-app (badge/banner) della disponibilità di un aggiornamento, opzione utente per abilitare il controllo periodico opt-in, logica di "silenzia per versione".

### Modified Capabilities
- `app-update`: il requisito "Controllo di nuove versioni su richiesta" viene ampliato per ammettere, in aggiunta al controllo manuale esistente, un controllo periodico **opt-in** (disattivato di default) innescato dall'apertura dell'app e limitato a un intervallo minimo tra due controlli; resta vietato qualunque controllo di rete se l'opzione non è stata attivata dall'utente.
- `app-settings`: la sezione "Controllo aggiornamenti" delle Impostazioni si arricchisce di un nuovo switch "Avvisami di nuove versioni" per abilitare/disabilitare il controllo opt-in descritto sopra.

## Impact

- Impostazioni: nuova preference "Avvisami di nuove versioni" (store preferenze MAUI `Preferences`, come le altre opzioni già presenti in `app-settings`).
- Lista carte / shell app: nuovo elemento UI per il badge/banner e per la sua dismissione.
- Riuso del client di controllo versione già introdotto da `app-update` (nessuna nuova chiamata di rete oltre a quella già esistente, solo un nuovo innesco temporizzato e opt-in).
- Nessun impatto su backend/v2, nessuna nuova dipendenza esterna.
