## ADDED Requirements

### Requirement: Rilettura tramite OpenAI con client sostituibile

La rilettura assistita SHALL usare l'API OpenAI con il modello selezionato nelle impostazioni e
output JSON strutturato conforme allo schema della ricevuta. Il reader dello scontrino e la
verifica della chiave MUST dipendere da un'astrazione `IAClient`, non direttamente dal trasporto
HTTP. La configurazione, inclusa la chiave API, SHALL essere passata al costruttore dell'istanza
client e i metodi successivi MUST usare quella configurazione senza ricevere nuovamente la chiave.

La chiave API SHALL essere quella fornita dall'utente e conservata nel Secure Storage del device;
non deve essere inclusa nel codice, nell'APK, nel database o nel backup.

#### Scenario: Invio dell'immagine a OpenAI

- **WHEN** l'utente conferma la rilettura di uno scontrino che non quadra
- **THEN** l'app invia l'immagine all'API OpenAI usando il modello selezionato e interpreta solo
  una risposta JSON conforme allo schema della ricevuta

#### Scenario: Verifica della chiave OpenAI

- **WHEN** l'utente chiede di verificare la chiave nelle impostazioni
- **THEN** l'app usa `IAClient` per effettuare una richiesta autenticata senza inviare
  immagini né dati dello scontrino

#### Scenario: Client sostituibile

- **WHEN** il reader o il verificatore viene eseguito in un test
- **THEN** è possibile fornire un'implementazione alternativa di `IAClient` senza usare la
  rete reale
