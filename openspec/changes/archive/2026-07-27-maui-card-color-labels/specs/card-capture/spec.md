## ADDED Requirements

### Requirement: Colore e label opzionali alla creazione della carta

Nella schermata di conferma che precede il salvataggio — qualunque sia il percorso di acquisizione (camera, immagine esistente, inserimento manuale) — il sistema SHALL consentire di scegliere il **colore del riquadro** dalla palette predefinita (o lasciare "Automatico") e di assegnare **label** secondo le regole della capability `card-labels`. Entrambi SHALL essere **opzionali**: il flusso di acquisizione MUST NOT richiedere di compilarli e i valori di default ("Automatico", nessuna label) SHALL consentire il salvataggio come oggi.

#### Scenario: Salvataggio senza toccare colore e label

- **WHEN** l'utente conferma il salvataggio di una carta valida senza scegliere colore né label
- **THEN** la carta viene salvata come oggi, con colore automatico derivato dal nome e nessuna label

#### Scenario: Colore scelto in creazione

- **WHEN** l'utente sceglie un colore dalla palette prima di salvare
- **THEN** la carta viene salvata con quel colore e il suo riquadro nella lista lo usa

#### Scenario: Label assegnate in creazione

- **WHEN** l'utente assegna una o più label prima di salvare
- **THEN** la carta viene salvata con quelle label, disponibili subito come filtro nella lista

#### Scenario: Suggerimenti disponibili in creazione

- **WHEN** l'utente apre l'editor delle label mentre crea una carta e altre carte hanno già delle label
- **THEN** quelle label compaiono come suggerimenti selezionabili con un tocco

#### Scenario: Carta ricevuta via QR di condivisione

- **WHEN** la schermata di conferma è pre-compilata dallo snapshot di un QR di condivisione ricevuto
- **THEN** colore ed etichette si comportano come per una carta nuova (colore automatico, nessuna label), e l'utente può impostarli prima di salvare
