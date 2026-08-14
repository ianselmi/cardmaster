## ADDED Requirements

### Requirement: Le letture di analisi sono aggregazioni nel database

Le letture che alimentano viste di analisi su tutto lo storico SHALL essere eseguite come **aggregazioni nel database**, restituendo il risultato già raggruppato. Il sistema MUST NOT caricare in memoria l'intero storico di scontrini o di righe per aggregarlo nel codice applicativo.

Il motivo è che il costo di un'aggregazione in memoria non si manifesta durante lo sviluppo, dove i dati di prova sono pochi, ma sul device di chi usa l'app da mesi: cresce in silenzio a ogni scontrino aggiunto.

Il database SHALL disporre degli indici che rendono sostenibili le aggregazioni ricorrenti al crescere dello storico. Gli indici di sola lettura MUST NOT richiedere un incremento della versione dello schema: non alterano la forma dei dati e non riguardano la guardia di compatibilità del ripristino.

#### Scenario: Aggregazione su tutto lo storico

- **WHEN** una vista di analisi richiede un totale raggruppato su tutte le righe di tutti gli scontrini
- **THEN** il risultato arriva già aggregato dal database, senza che le singole righe dello storico vengano caricate in memoria

#### Scenario: Indice aggiunto senza cambio di schema

- **WHEN** l'app si apre su un database esistente e crea un indice mancante per le aggregazioni
- **THEN** i dati restano invariati, la versione dello schema non cambia e i backup esistenti restano ripristinabili

#### Scenario: Crescita dello storico

- **WHEN** lo storico contiene un numero di scontrini e di righe pari a quello di un uso prolungato dell'app
- **THEN** le viste di analisi restano utilizzabili senza attese percepibili
