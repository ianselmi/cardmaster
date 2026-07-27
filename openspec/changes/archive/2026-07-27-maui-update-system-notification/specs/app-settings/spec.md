## MODIFIED Requirements

### Requirement: Sezione Controllo aggiornamenti nelle Impostazioni

Il sistema SHALL fornire, all'interno delle Impostazioni, una sezione dedicata al controllo degli aggiornamenti, raggiungibile dalla pagina Impostazioni. La sezione SHALL mostrare l'esito dell'ultimo controllo effettuato e SHALL esporre l'azione "Verifica aggiornamenti". La sezione SHALL inoltre esporre uno switch **"Avvisami di nuove versioni"** (default disattivato) per abilitare/disabilitare il controllo automatico opt-in, il segnale in-app e la **notifica di sistema**, il cui comportamento di dettaglio è definito dalla capability `app-update-notify`. La descrizione dello switch SHALL dichiarare che il controllo avviene anche **ad app chiusa**, così che l'utente sappia che sta abilitando un'attività di rete periodica. All'attivazione dello switch il sistema SHALL richiedere il permesso di inviare notifiche. Il comportamento di dettaglio del controllo, download e installazione è definito nella capability `app-update`.

L'esito mostrato NON MUST annunciare come disponibile una versione che coincide con quella **installata**: in quel caso la sezione SHALL riportare l'ultimo controllo come "nessun aggiornamento disponibile", conservando la data/ora in cui il controllo è avvenuto.

#### Scenario: Apertura della sezione controllo aggiornamenti

- **WHEN** l'utente apre la sezione "Controllo aggiornamenti" dalle Impostazioni
- **THEN** il sistema mostra l'esito dell'ultimo controllo (se presente), l'azione "Verifica aggiornamenti" e lo switch "Avvisami di nuove versioni"

#### Scenario: Nessun controllo ancora effettuato

- **WHEN** l'utente apre la sezione senza aver mai avviato un controllo aggiornamenti
- **THEN** la sezione non mostra un esito precedente e offre comunque l'azione "Verifica aggiornamenti", con lo switch "Avvisami di nuove versioni" disattivato di default

#### Scenario: Attivazione dello switch

- **WHEN** l'utente attiva lo switch "Avvisami di nuove versioni"
- **THEN** il sistema persiste la preferenza, richiede il permesso di inviare notifiche e da quel momento abilita il controllo automatico descritto da `app-update-notify`

#### Scenario: Descrizione dello switch esplicita sul controllo in background

- **WHEN** l'utente legge la descrizione dello switch "Avvisami di nuove versioni"
- **THEN** la descrizione dichiara che il controllo avviene anche ad app chiusa e che verrà mostrata una notifica

#### Scenario: Esito dopo l'installazione dell'aggiornamento annunciato

- **WHEN** l'ultimo controllo aveva annunciato la versione N e l'utente ha nel frattempo installato la versione N
- **THEN** la sezione riporta l'ultimo controllo come "nessun aggiornamento disponibile", con la data/ora del controllo realmente effettuato, e non annuncia la versione N come disponibile
