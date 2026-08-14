## ADDED Requirements

### Requirement: Ricostruzione delle righe prodotto dalla geometria del testo

Il sistema SHALL ricostruire le righe prodotto di uno scontrino separando **descrizione** e **importo** in base alla **posizione orizzontale** dei frammenti di testo riconosciuti, non in base al loro ordine nella riga. La colonna degli importi SHALL essere individuata sullo **scontrino nel suo insieme**, dalla distribuzione dei bordi destri degli importi candidati, e non riga per riga: un numero che cade fuori dalla colonna degli importi MUST essere trattato come parte della descrizione. La ricostruzione delle righe visive SHALL riusare quella già impiegata per l'estrazione della testata, e il comportamento della testata MUST NOT cambiare.

#### Scenario: Descrizione e importo separati per colonna

- **WHEN** una riga contiene una descrizione a sinistra e un importo nella colonna di destra
- **THEN** la riga prodotto risultante ha come descrizione il solo testo di sinistra e come importo quello della colonna di destra

#### Scenario: Numero dentro la descrizione non scambiato per prezzo

- **WHEN** una riga contiene un numero nella descrizione (per esempio una grammatura) e un importo nella colonna di destra
- **THEN** il numero della descrizione resta nella descrizione e l'importo di riga è quello della colonna di destra

#### Scenario: Riga senza importo in colonna

- **WHEN** una riga del corpo non ha alcun frammento numerico nella colonna degli importi
- **THEN** il sistema non le attribuisce alcun importo e non inventa un prezzo

#### Scenario: Estrazione della testata invariata

- **WHEN** viene analizzato uno scontrino già supportato prima di questa capability
- **THEN** esercente, partita IVA, data e totale estratti sono identici a quelli estratti in precedenza

### Requirement: Delimitazione del corpo dello scontrino

Il sistema SHALL cercare le righe prodotto **soltanto** tra la fine dell'intestazione e la riga del totale (o del primo subtotale, se precede il totale). Le righe che stanno fuori da questo intervallo MUST NOT essere proposte come prodotti, anche quando ne hanno la forma. Quando la riga del totale non è individuabile, il sistema MUST NOT estendere il corpo fino alla fine dello scontrino.

#### Scenario: Coda dello scontrino esclusa

- **WHEN** dopo il totale lo scontrino riporta pagamento, resto, riepilogo IVA e punti fedeltà
- **THEN** nessuna di quelle righe compare tra le righe prodotto

#### Scenario: Intestazione esclusa

- **WHEN** l'intestazione contiene indirizzo, partita IVA e numeri di telefono
- **THEN** nessuna di quelle righe compare tra le righe prodotto

#### Scenario: Totale non individuato

- **WHEN** lo scontrino non ha una riga di totale riconoscibile
- **THEN** il sistema non propone righe prodotto ricavate dalla coda dello scontrino e segnala che le righe non sono state ricostruite

### Requirement: Interpretazione dei casi ricorrenti degli scontrini italiani

Il sistema SHALL riconoscere, con regole distinte applicate in ordine, le forme che sugli scontrini italiani sono la norma:

- **quantità esplicita** (`2 X 1,50`, `2 PZ x 1,50`): la riga porta quantità e prezzo unitario, e l'importo di riga è il loro prodotto;
- **prodotto a peso** (`0,432 kg x 2,99 €/kg`): stessa forma con quantità frazionaria, conservata in **millesimi interi**;
- **sconto o promozione** (importo negativo, oppure riga marcata come sconto/promozione): riga con importo negativo che MUST NOT essere trattata come prodotto;
- **continuazione della descrizione**: riga priva di importo in colonna, immediatamente successiva a una riga prodotto, che SHALL essere accodata alla descrizione precedente invece di generare una riga a sé;
- **riga di servizio** (reparto, codice, conteggio pezzi, riepilogo IVA): MUST NOT diventare una riga prodotto.

Quando la quantità non è indicata, il sistema SHALL assumere **1** e MUST NOT dedurre quantità non presenti sullo scontrino.

#### Scenario: Quantità per prezzo unitario

- **WHEN** una riga indica due unità a 1,50 ciascuna
- **THEN** la riga prodotto ha quantità 2, prezzo unitario 1,50 e importo 3,00

#### Scenario: Totale di riga incoerente con quantità e prezzo unitario

- **WHEN** lo scontrino riporta anche il totale della riga e questo non coincide con quantità per prezzo unitario
- **THEN** la riga viene segnalata come incoerente invece di essere corretta d'ufficio

#### Scenario: Prodotto venduto a peso

- **WHEN** una riga indica 0,432 kg a 2,99 €/kg
- **THEN** la riga prodotto conserva la quantità come 432 millesimi e l'importo calcolato dal peso

#### Scenario: Sconto con importo negativo

- **WHEN** il corpo contiene una riga di sconto con importo negativo
- **THEN** la riga è registrata come sconto, non come prodotto, e il suo importo negativo concorre alla somma delle righe

#### Scenario: Descrizione mandata a capo

- **WHEN** la descrizione di un prodotto occupa due righe e solo la prima ha un importo
- **THEN** ne risulta una sola riga prodotto con la descrizione completa, e nessuna riga con prezzo nullo

#### Scenario: Riga di servizio scartata

- **WHEN** il corpo contiene una riga di reparto o di conteggio pezzi
- **THEN** quella riga non compare tra le righe prodotto

### Requirement: Aliquota IVA della riga

Il sistema SHALL leggere l'**aliquota IVA** di ogni riga prodotto dal campo che gli scontrini italiani stampano tra la descrizione e il prezzo, sia quando riporta l'aliquota per esteso sia quando riporta un **codice di reparto**. Per risolvere i codici il sistema SHALL leggere il **riepilogo IVA** a piè di scontrino, ricavandone la corrispondenza codice → aliquota e il **totale dell'imposta**; il riepilogo MUST NOT generare righe prodotto. L'aliquota SHALL essere conservata in **punti base interi** (`4,00%` → `400`), mai in virgola mobile.

Quando l'aliquota non è leggibile, o il codice non compare nel riepilogo, la riga SHALL restare **senza aliquota**. Il sistema MUST NOT dedurre l'aliquota dalla categoria del prodotto né assumere un valore di default.

#### Scenario: Aliquota stampata per esteso

- **WHEN** la riga riporta l'aliquota accanto al prezzo
- **THEN** la riga prodotto porta quell'aliquota e il valore non finisce nella descrizione né nell'importo

#### Scenario: Codice di reparto risolto dal riepilogo

- **WHEN** la riga riporta un codice di reparto e il riepilogo IVA associa quel codice a un'aliquota
- **THEN** la riga prodotto porta l'aliquota corrispondente

#### Scenario: Codice non risolvibile

- **WHEN** la riga riporta un codice di reparto assente dal riepilogo, o il riepilogo non è leggibile
- **THEN** la riga resta senza aliquota, e nessuna aliquota viene assunta per default

#### Scenario: Aliquota non dedotta dalla categoria

- **WHEN** una riga è classificata in una categoria ma non ha aliquota leggibile
- **THEN** la riga resta senza aliquota

#### Scenario: Riepilogo IVA non confuso con i prodotti

- **WHEN** lo scontrino riporta il riepilogo IVA con imponibili e imposte per reparto
- **THEN** quelle voci non compaiono tra le righe prodotto, ma la loro informazione è usata per aliquote e totale imposta

### Requirement: Quadratura delle righe rispetto al totale

Il sistema SHALL confrontare la **somma delle righe** (sconti compresi) con il **totale della testata** e SHALL dichiarare l'esito del confronto. La tolleranza SHALL essere di **zero centesimi**. Quando la somma non coincide, il sistema SHALL indicare **di quanto** differisce e invitare alla correzione; MUST NOT aggiungere righe fittizie di differenza, MUST NOT correggere alcun importo d'ufficio e MUST NOT impedire il salvataggio. Quando il totale di testata è assente, il sistema SHALL dichiarare le righe **non validate** invece di dichiararle quadrate.

#### Scenario: Somma coincidente

- **WHEN** la somma delle righe ricostruite coincide al centesimo con il totale
- **THEN** il sistema dichiara che le righe quadrano con il totale

#### Scenario: Somma non coincidente

- **WHEN** la somma delle righe differisce dal totale
- **THEN** il sistema mostra la differenza e invita a correggere le righe, senza modificarle da solo

#### Scenario: Salvataggio con quadratura fallita

- **WHEN** l'utente conferma uno scontrino le cui righe non quadrano
- **THEN** lo scontrino e le righe vengono salvati e la mancata quadratura resta visibile nel dettaglio

#### Scenario: Totale di testata assente

- **WHEN** lo scontrino non ha un totale di testata
- **THEN** il sistema dichiara le righe non validate e non afferma che quadrano

### Requirement: Quadratura per aliquota

Quando il **riepilogo IVA** è leggibile, il sistema SHALL confrontare, **per ciascuna aliquota**, la somma delle righe che la portano con l'importo **lordo** dichiarato nel riepilogo per quell'aliquota — imponibile più imposta, non il solo imponibile, perché i prezzi di riga di uno scontrino italiano sono IVA inclusa. Valgono le stesse regole della quadratura sul totale: tolleranza **zero centesimi**, scarto dichiarato, nessuna correzione d'ufficio, nessun blocco del salvataggio. Il sistema SHALL segnalare **quale aliquota** non torna, non solo che qualcosa non torna. Quando il riepilogo non è leggibile, la quadratura per aliquota SHALL essere dichiarata **non effettuata**, e MUST NOT far apparire come verificate righe che non lo sono.

#### Scenario: Errori che si compensano

- **WHEN** due righe di aliquote diverse sono lette con errori di segno opposto che lasciano corretto il totale complessivo
- **THEN** la quadratura per aliquota segnala la discrepanza, indicando le aliquote coinvolte

#### Scenario: Riepilogo assente

- **WHEN** il riepilogo IVA non è leggibile sullo scontrino
- **THEN** il sistema dichiara la quadratura per aliquota non effettuata, e resta valida la sola quadratura sul totale

#### Scenario: Righe senza aliquota

- **WHEN** alcune righe sono rimaste senza aliquota
- **THEN** il sistema lo dichiara invece di attribuirle a un'aliquota per farle quadrare

### Requirement: Correzione manuale delle righe

Il sistema SHALL permettere di correggere le righe a mano, sia nella schermata di conferma sia su uno scontrino **già salvato**: modificare descrizione, quantità e importo di una riga, **aggiungere** una riga che il riconoscimento non ha letto, **eliminare** una riga inesistente. Ogni correzione SHALL aggiornare immediatamente la quadratura mostrata. Uno scontrino le cui righe sono state riconosciute male SHALL restare salvabile e correggibile per intero.

#### Scenario: Importo corretto a mano

- **WHEN** l'utente corregge l'importo di una riga
- **THEN** la riga viene salvata con il valore corretto e la quadratura viene ricalcolata

#### Scenario: Riga mancante aggiunta

- **WHEN** l'utente aggiunge a mano una riga che il riconoscimento aveva perso
- **THEN** la riga entra nello scontrino e concorre alla somma

#### Scenario: Riga inventata eliminata

- **WHEN** l'utente elimina una riga che il riconoscimento aveva prodotto per errore
- **THEN** la riga sparisce dallo scontrino e dalla somma

#### Scenario: Correzione di uno scontrino già salvato

- **WHEN** l'utente modifica le righe di uno scontrino salvato in precedenza e conferma
- **THEN** le modifiche sono persistite e il dettaglio le mostra alla riapertura

#### Scenario: Riconoscimento completamente fallito

- **WHEN** nessuna riga viene ricostruita dallo scontrino
- **THEN** l'utente può inserire le righe a mano e salvare lo scontrino ugualmente

### Requirement: Righe visibili nel dettaglio dello scontrino

Il dettaglio di uno scontrino salvato SHALL mostrare le sue righe prodotto — descrizione, quantità, **aliquota IVA** e importo — insieme al **segnale di quadratura** rispetto al totale e, quando disponibile, **per aliquota**. Uno scontrino privo di righe SHALL restare consultabile come oggi, senza sezioni vuote né messaggi di errore.

#### Scenario: Dettaglio con righe

- **WHEN** l'utente apre uno scontrino che ha righe salvate
- **THEN** vede le righe con descrizione, quantità, aliquota e importo, e se quadrano con il totale

#### Scenario: Riga senza aliquota nel dettaglio

- **WHEN** una riga è rimasta senza aliquota
- **THEN** il dettaglio lo mostra come campo vuoto, distinguibile da un'aliquota letta

#### Scenario: Scontrino senza righe

- **WHEN** l'utente apre uno scontrino salvato prima di questa capability, o salvato senza righe
- **THEN** il dettaglio mostra i dati di testata come prima, senza sezione righe vuota né errori

### Requirement: Persistenza delle righe prodotto

Il sistema SHALL persistere le righe come **entità figlie** dello scontrino, in una tabella propria interrogabile, con `Id` generato dal client e cancellazione logica tramite tombstone come le altre entità. Gli importi SHALL essere conservati come **interi in centesimi**, le quantità come **interi in millesimi** e le aliquote come **interi in punti base**: nessun valore in virgola mobile lungo il percorso. Ogni riga SHALL conservare la **descrizione normalizzata** accanto a quella grezza, così che le aggregazioni per prodotto non dipendano da una tabella che l'utente può riscrivere. Il salvataggio di uno scontrino SHALL **sostituire in blocco** le sue righe precedenti. L'eliminazione di uno scontrino SHALL eliminare logicamente anche le sue righe. La **versione di schema** del database SHALL essere incrementata, perché un backup contenente le righe MUST NOT essere ripristinabile da una versione dell'app che non le conosce.

Ogni riga SHALL conservare inoltre l'**unità di misura** della quantità (pezzo o peso): senza di essa `2 pz` e `0,002 kg` sono lo stesso numero di millesimi, e i prezzi unitari non sono confrontabili tra scontrini.

#### Scenario: Unità di misura distinta

- **WHEN** uno scontrino contiene sia una riga a pezzi sia una riga a peso
- **THEN** le due righe risultano con unità di misura diverse, e il loro prezzo unitario non viene confrontato come se fosse omogeneo

#### Scenario: Descrizione normalizzata disponibile per l'aggregazione

- **WHEN** lo stesso prodotto compare su scontrini diversi con la stessa descrizione grezza
- **THEN** le righe portano la stessa descrizione normalizzata, e restano raggruppabili anche se le mappature di categoria vengono modificate

#### Scenario: Righe salvate con lo scontrino

- **WHEN** l'utente conferma uno scontrino con le sue righe
- **THEN** le righe sono persistite localmente e sono rilette alla riapertura del dettaglio

#### Scenario: Righe sostituite a una nuova modifica

- **WHEN** l'utente modifica le righe di uno scontrino salvato e conferma
- **THEN** lo scontrino risulta con le sole righe correnti, senza duplicati delle precedenti

#### Scenario: Righe eliminate con lo scontrino

- **WHEN** l'utente elimina uno scontrino
- **THEN** le sue righe spariscono con lui e restano nel database come tombstone

#### Scenario: Nessuna perdita di precisione

- **WHEN** si sommano gli importi delle righe di uno scontrino
- **THEN** il risultato è esatto al centesimo, senza errori di arrotondamento

#### Scenario: Backup più recente dello schema rifiutato

- **WHEN** si tenta di ripristinare su una versione precedente dell'app un backup prodotto con le righe
- **THEN** il ripristino viene rifiutato dalla guardia di compatibilità già esistente

### Requirement: Nessuna funzione di rete per le righe prodotto

La ricostruzione, la correzione, la persistenza e la consultazione delle righe MUST funzionare interamente offline. Questa capability MUST NOT introdurre alcuna chiamata di rete, alcuna dipendenza nuova, alcun permesso nuovo, né alcuna credenziale o chiave incorporata nel codice o nel pacchetto dell'applicazione.

#### Scenario: Funzionamento in modalità aereo

- **WHEN** il device è in modalità aereo
- **THEN** righe ricostruite, correzione, quadratura e consultazione funzionano senza alcuna differenza

#### Scenario: Nessun dato delle righe lascia il device

- **WHEN** le righe di uno scontrino vengono ricostruite e salvate
- **THEN** nessuna descrizione, quantità o importo viene trasmesso ad alcun servizio esterno
