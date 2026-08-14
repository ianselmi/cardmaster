## MODIFIED Requirements

### Requirement: Spesa per esercente e per mese

Il sistema SHALL mostrare, dai soli dati di testata, il totale speso per **mese** e la sua ripartizione per **esercente**. Gli scontrini privi di data o di totale MUST NOT falsare i totali e SHALL essere segnalati come incompleti. Importi e date SHALL essere formattati in euro e in italiano, indipendentemente dalla lingua configurata sul device.

Questa aggregazione SHALL essere calcolata dal database e non ricavata caricando gli scontrini in memoria, ed è la **stessa** che alimenta la vista di analisi su tutto lo storico: il riepilogo mostrato accanto alla lista degli scontrini e la vista estesa MUST NOT avere implementazioni distinte, per non divergere al primo aggiustamento.

#### Scenario: Totale mensile per esercente

- **WHEN** l'utente apre la vista di spesa con più scontrini di negozi diversi nello stesso mese
- **THEN** vede il totale del mese e quanto è stato speso presso ciascun esercente

#### Scenario: Scontrino incompleto escluso dai totali

- **WHEN** uno scontrino salvato non ha data o non ha totale
- **THEN** non viene conteggiato nei totali e l'utente può individuarlo per completarlo

#### Scenario: Formato indipendente dalla lingua del device

- **WHEN** il device è configurato in una lingua diversa dall'italiano
- **THEN** importi e date restano nel formato italiano in euro

#### Scenario: Stesso risultato accanto alla lista e nella vista estesa

- **WHEN** l'utente confronta il riepilogo del mese mostrato accanto alla lista degli scontrini con lo stesso mese nella vista di analisi
- **THEN** i due valori coincidono, perché provengono dalla stessa aggregazione
