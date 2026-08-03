## ADDED Requirements

### Requirement: Backup della situazione corrente prima del ripristino

Prima di sostituire il database durante un ripristino, il sistema SHALL eseguire un **backup ordinario della situazione corrente** nella cartella applicativa di Drive: stesso contenuto, stessa convenzione di nome e stessa ritenzione dei backup normali, così da comparire nella lista dei backup ed essere ripristinabile in qualsiasi momento come qualunque altro. Se questo backup non riesce, il sistema MUST NOT sostituire il database: SHALL interrompere il ripristino lasciando invariato il database corrente e SHALL mostrare il messaggio della categoria d'errore corrispondente.

#### Scenario: Backup della situazione corrente prima della sostituzione

- **WHEN** l'utente conferma un ripristino
- **THEN** il sistema carica su Drive un backup del database corrente e solo dopo sostituisce il database con il backup scelto

#### Scenario: Il backup pre-ripristino compare nella lista

- **WHEN** l'utente riapre la lista dei backup dopo un ripristino
- **THEN** il backup della situazione precedente al ripristino è elencato come tutti gli altri e può essere ripristinato a sua volta

#### Scenario: Backup pre-ripristino non riuscito

- **WHEN** il backup della situazione corrente fallisce (rete assente, spazio Drive esaurito, credenziali non più valide o errore locale)
- **THEN** il ripristino non viene eseguito, il database corrente resta invariato e l'utente vede il messaggio della categoria d'errore

#### Scenario: Il backup pre-ripristino rientra nella ritenzione

- **WHEN** il backup della situazione corrente porta il totale oltre il limite di ritenzione
- **THEN** il sistema elimina i backup più vecchi come per un backup qualsiasi

## MODIFIED Requirements

### Requirement: Ripristino da un backup precedente

Il sistema SHALL elencare **in-app** i backup disponibili su Drive (data e dimensione) e permettere all'utente di sceglierne uno da ripristinare. Il ripristino SHALL **sostituire l'intero database** con il contenuto del backup scelto. Prima di sostituire, il sistema SHALL chiedere una **conferma esplicita** dell'operazione distruttiva, dichiarando che la situazione corrente viene prima salvata su Drive come backup. Il sistema MUST NOT ripristinare un backup con **versione di schema più recente** di quella supportata dall'app installata.

#### Scenario: Lista dei backup in-app

- **WHEN** l'utente apre "Ripristina da un backup…"
- **THEN** il sistema mostra l'elenco dei backup disponibili con data e dimensione, letti dalla cartella applicativa di Drive

#### Scenario: Ripristino con conferma

- **WHEN** l'utente sceglie un backup e conferma la sostituzione
- **THEN** il sistema salva su Drive la situazione corrente, scarica il backup, sostituisce l'intero database e i dati ripristinati diventano visibili nell'app

#### Scenario: Conferma richiesta prima della sostituzione

- **WHEN** l'utente sceglie un backup ma non conferma
- **THEN** il database corrente resta invariato e nessun backup viene caricato su Drive

#### Scenario: La conferma dichiara il backup della situazione corrente

- **WHEN** il sistema chiede conferma del ripristino
- **THEN** il messaggio dichiara che la situazione corrente viene salvata su Drive come backup prima della sostituzione

#### Scenario: Blocco del downgrade di schema

- **WHEN** l'utente sceglie un backup la cui versione di schema è più recente di quella dell'app installata
- **THEN** il sistema rifiuta il ripristino con un messaggio chiaro e lascia invariato il database corrente

## REMOVED Requirements

### Requirement: Snapshot di sicurezza prima del ripristino

**Reason**: La copia di sicurezza locale in cache era una seconda rete di sicurezza, invisibile e non ripristinabile dalla lista: viveva solo nella cache del device (ripulibile da Android) e solo finché il processo dell'app restava vivo. Il backup della situazione corrente caricato su Drive copre lo stesso bisogno con il meccanismo che l'utente già conosce e vede.

**Migration**: Per tornare allo stato precedente a un ripristino, l'utente ripristina dalla lista in-app il backup caricato immediatamente prima della sostituzione, invece di usare l'azione "Annulla ripristino" mostrata subito dopo l'operazione. A differenza dell'undo, questa via resta disponibile anche dopo la chiusura dell'app e da un altro device.
