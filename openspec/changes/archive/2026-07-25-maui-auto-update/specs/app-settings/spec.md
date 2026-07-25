## ADDED Requirements

### Requirement: Sezione Controllo aggiornamenti nelle Impostazioni

Il sistema SHALL fornire, all'interno delle Impostazioni, una sezione dedicata al controllo degli aggiornamenti, raggiungibile dalla pagina Impostazioni. La sezione SHALL mostrare l'esito dell'ultimo controllo effettuato e SHALL esporre l'azione "Verifica aggiornamenti". Il comportamento di dettaglio del controllo, download e installazione è definito nella capability `app-update`.

#### Scenario: Apertura della sezione controllo aggiornamenti

- **WHEN** l'utente apre la sezione "Controllo aggiornamenti" dalle Impostazioni
- **THEN** il sistema mostra l'esito dell'ultimo controllo (se presente) e l'azione "Verifica aggiornamenti"

#### Scenario: Nessun controllo ancora effettuato

- **WHEN** l'utente apre la sezione senza aver mai avviato un controllo aggiornamenti
- **THEN** la sezione non mostra un esito precedente e offre comunque l'azione "Verifica aggiornamenti"
