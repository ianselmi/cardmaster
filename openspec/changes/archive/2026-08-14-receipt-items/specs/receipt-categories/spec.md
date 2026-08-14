## ADDED Requirements

### Requirement: Classificazione di una riga prodotto da dizionario locale

Il sistema SHALL assegnare a ogni riga prodotto una **categoria di spesa** consultando, in quest'ordine, le **mappature apprese** dall'utente e poi un **dizionario seed** incluso nel pacchetto dell'applicazione. Il dizionario seed SHALL essere un dato versionato con l'app, non scaricato a runtime, e SHALL coprire poche categorie larghe della spesa alimentare e domestica. Il confronto SHALL avvenire sulla descrizione **normalizzata** con la stessa regola di normalizzazione già usata altrove nell'app, e SHALL tollerare le abbreviazioni tipiche degli scontrini confrontando **token contenuti e prefissi**. Il sistema MUST NOT usare una distanza di edit generica, che accoppierebbe descrizioni brevi di significato diverso.

#### Scenario: Prodotto riconosciuto dal seed

- **WHEN** una riga prodotto contiene una parola-chiave presente nel dizionario seed
- **THEN** alla riga viene assegnata la categoria corrispondente

#### Scenario: Descrizione abbreviata

- **WHEN** la descrizione dello scontrino è abbreviata o puntata rispetto alla parola-chiave del dizionario
- **THEN** la corrispondenza viene comunque riconosciuta se il token della descrizione è prefisso della parola-chiave

#### Scenario: Nessuna corrispondenza inventata su parole simili

- **WHEN** la descrizione somiglia a una parola-chiave ma non la contiene né la prefissa
- **THEN** il sistema non assegna quella categoria

#### Scenario: Dizionario disponibile offline al primo avvio

- **WHEN** l'app viene usata per la prima volta su un device mai stato online
- **THEN** la classificazione funziona, perché il dizionario è già nel pacchetto

### Requirement: Riga senza categoria quando non c'è corrispondenza

Quando nessuna sorgente riconosce la descrizione, la riga SHALL restare **senza categoria** ed essere mostrata come tale. Il sistema MUST NOT assegnare d'ufficio una categoria generica di ripiego, che sarebbe indistinguibile da una classificazione riuscita.

#### Scenario: Prodotto sconosciuto

- **WHEN** la descrizione di una riga non corrisponde a nulla nelle mappature né nel seed
- **THEN** la riga risulta senza categoria e l'utente lo vede

#### Scenario: Nessuna categoria di ripiego

- **WHEN** si esaminano le righe non classificate di uno scontrino
- **THEN** nessuna di esse porta una categoria assegnata automaticamente

### Requirement: Apprendimento locale dalle correzioni dell'utente

Quando l'utente **corregge la categoria** di una riga, il sistema SHALL registrare una **mappatura locale** dalla descrizione normalizzata alla categoria scelta, e SHALL applicarla agli scontrini acquisiti **successivamente**. Le mappature apprese SHALL avere **precedenza** sul dizionario seed. Correggere di nuovo la categoria dello stesso prodotto SHALL **riscrivere** la mappatura esistente, non aggiungerne una seconda. Le mappature MUST NOT essere applicate retroattivamente agli scontrini già salvati.

#### Scenario: Correzione applicata agli scontrini successivi

- **WHEN** l'utente corregge la categoria di un prodotto e in seguito acquisisce un altro scontrino con lo stesso prodotto
- **THEN** quella riga risulta già classificata con la categoria scelta dall'utente

#### Scenario: Mappatura appresa prevalente sul seed

- **WHEN** un prodotto è presente nel dizionario seed e l'utente gli ha assegnato una categoria diversa
- **THEN** vale la categoria scelta dall'utente

#### Scenario: Seconda correzione dello stesso prodotto

- **WHEN** l'utente corregge di nuovo la categoria di un prodotto già corretto in passato
- **THEN** la mappatura viene aggiornata e ne resta una sola per quel prodotto

#### Scenario: Scontrini passati non riscritti

- **WHEN** l'utente corregge la categoria di un prodotto presente anche in scontrini già salvati
- **THEN** quegli scontrini restano con la classificazione che avevano

### Requirement: Persistenza delle mappature prodotto → categoria

Il sistema SHALL persistere le mappature apprese in una tabella locale con `Id` generato dal client e cancellazione logica tramite tombstone, come le altre entità. Ogni mappatura SHALL conservare la **descrizione normalizzata** come chiave, la **categoria**, un **nome visualizzato** normalizzato e l'**origine** della mappatura, così da distinguere una scelta dell'utente da una prodotta automaticamente. Una mappatura di origine utente MUST NOT essere sovrascritta da una prodotta automaticamente. Le mappature SHALL sopravvivere agli scontrini da cui sono nate: l'eliminazione di uno scontrino MUST NOT eliminarle.

#### Scenario: Mappature conservate tra le sessioni

- **WHEN** l'utente chiude e riapre l'app dopo aver corretto alcune categorie
- **THEN** le correzioni sono ancora in vigore

#### Scenario: Mappatura indipendente dallo scontrino di origine

- **WHEN** l'utente elimina lo scontrino su cui aveva corretto una categoria
- **THEN** la mappatura resta valida per gli scontrini successivi

#### Scenario: Origine della mappatura registrata

- **WHEN** si esamina una mappatura nata da una correzione dell'utente
- **THEN** risulta di origine utente e non viene sostituita da una classificazione automatica

### Requirement: Classificazione interamente offline e senza segreti

La classificazione MUST funzionare interamente offline: nessuna chiamata di rete, nessun modello scaricato a runtime, nessuna credenziale o chiave incorporata nel codice sorgente o nel pacchetto dell'applicazione.

#### Scenario: Classificazione in modalità aereo

- **WHEN** il device è in modalità aereo
- **THEN** righe classificate, correzione della categoria e apprendimento funzionano senza alcuna differenza

#### Scenario: Nessun dato di classificazione trasmesso

- **WHEN** una riga viene classificata o una categoria viene corretta
- **THEN** né la descrizione né la categoria vengono trasmesse ad alcun servizio esterno
