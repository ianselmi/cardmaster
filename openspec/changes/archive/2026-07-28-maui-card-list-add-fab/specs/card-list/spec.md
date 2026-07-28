## ADDED Requirements

### Requirement: Bottone flottante per aggiungere una carta

La lista carte SHALL presentare l'azione di aggiunta carta come un **bottone tondo** con il simbolo `+`, posizionato **in basso al centro** della pagina e **sovrapposto** al contenuto della lista, così da non ridurre lo spazio verticale disponibile per la griglia. Il bottone MUST restare visibile e nella stessa posizione mentre si scorre la lista, MUST essere presente anche quando la lista è vuota o filtrata a zero risultati, e MUST usare il colore d'accento di brand con il simbolo a contrasto leggibile in tema chiaro e scuro. Il tocco SHALL aprire lo stesso flusso di acquisizione carta raggiunto in precedenza dalla toolbar, senza alcuna modifica al flusso stesso.

#### Scenario: Bottone presente in basso al centro

- **WHEN** l'utente apre la lista carte
- **THEN** in basso al centro compare un bottone tondo con il simbolo `+`, sopra il contenuto della lista

#### Scenario: Il bottone apre l'acquisizione carta

- **WHEN** l'utente tocca il bottone tondo `+`
- **THEN** si apre la schermata di scansione/acquisizione di una nuova carta, come faceva la voce "Aggiungi" della toolbar

#### Scenario: Posizione stabile durante lo scorrimento

- **WHEN** l'utente scorre la griglia delle carte
- **THEN** il bottone resta fermo in basso al centro e tocabile, senza scorrere via con la lista

#### Scenario: Disponibile anche a lista vuota

- **WHEN** non ci sono carte salvate, oppure ricerca e filtri non producono risultati
- **THEN** il bottone tondo `+` è comunque presente e permette di aggiungere una carta

#### Scenario: La griglia non perde altezza

- **WHEN** si confronta lo spazio verticale occupato dalla griglia prima e dopo l'introduzione del bottone
- **THEN** la griglia dispone della stessa altezza utile (il bottone è sovrapposto, non incolonnato sotto la lista)

### Requirement: Toolbar della lista senza la voce "Aggiungi"

La toolbar della lista carte SHALL NOT contenere una voce testuale "Aggiungi": l'aggiunta carta è raggiungibile solo dal bottone flottante. La voce **Impostazioni** e il suo segnale di aggiornamento disponibile MUST restare invariati.

#### Scenario: Nessuna voce "Aggiungi" in toolbar

- **WHEN** l'utente guarda la toolbar della lista carte
- **THEN** non compare la voce "Aggiungi"; l'aggiunta di una carta si fa dal bottone tondo in basso

#### Scenario: Impostazioni e badge invariati

- **WHEN** è disponibile un aggiornamento e l'utente guarda la toolbar
- **THEN** la voce Impostazioni è presente col suo badge di aggiornamento e si comporta come prima
