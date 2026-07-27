## ADDED Requirements

### Requirement: Compattezza dell'area di ricerca e filtro

L'insieme degli elementi che precedono la griglia dei riquadri — barra di ricerca, conteggio, chip delle label e barra delle carte usate di recente — SHALL occupare il minimo spazio verticale compatibile con la loro funzione, così che la griglia disponga della maggior parte dell'altezza. In particolare: il conteggio NON MUST avere una riga dedicata e la barra delle carte usate di recente NON MUST essere preceduta da una riga di intestazione testuale.

Le dimensioni verticali di questi elementi SHALL derivare dalla scala di spaziatura condivisa definita da `visual-identity`, non da valori isolati.

A riposo (nessun filtro attivo, nessun banner di aggiornamento) la griglia SHALL cominciare entro il **primo terzo** dello spazio disponibile sotto la barra del titolo: l'area di ricerca e filtro NON MUST occuparne più del 33%.

#### Scenario: La griglia comincia entro il primo terzo

- **WHEN** l'utente apre la lista con carte salvate, almeno una label esistente, la barra dei recenti popolata, nessun filtro attivo e nessun banner di aggiornamento
- **THEN** la prima riga di riquadri comincia entro il primo terzo dell'altezza disponibile sotto la barra del titolo

#### Scenario: Due righe di riquadri visibili a riposo

- **WHEN** l'utente apre la lista con almeno quattro carte salvate e nessun filtro attivo
- **THEN** almeno due righe complete di riquadri sono visibili senza scorrere, **anche con il banner di aggiornamento presente**

#### Scenario: Nessuna riga dedicata al conteggio

- **WHEN** la lista mostra il conteggio
- **THEN** il conteggio condivide la riga con i chip delle label, senza occuparne una propria

#### Scenario: Barra dei recenti senza intestazione

- **WHEN** la barra delle carte usate di recente è visibile
- **THEN** non è preceduta da una riga di intestazione testuale, e resta distinguibile dalla griglia per la dimensione ridotta dei suoi riquadri

### Requirement: Gli elementi non visibili non occupano spazio

Quando un elemento dell'area di ricerca e filtro non è visibile — perché non esistono label, perché nessun filtro è attivo o perché nessuna carta è mai stata aperta — esso MUST NOT occupare spazio verticale, **inclusa la spaziatura** che lo separerebbe dagli elementi adiacenti.

#### Scenario: Nessuna label, nessuno spazio

- **WHEN** nessuna carta ha label assegnate
- **THEN** la riga dei chip non occupa spazio, né come contenuto né come spaziatura, e la griglia guadagna quello spazio

#### Scenario: Nessuna carta usata di recente

- **WHEN** nessuna carta è mai stata aperta
- **THEN** la barra delle carte usate di recente non occupa spazio, né come contenuto né come spaziatura

#### Scenario: Area di filtro interamente assente

- **WHEN** non esistono label e nessun filtro è attivo
- **THEN** la riga che ospita conteggio e chip non occupa spazio

## MODIFIED Requirements

### Requirement: Indicatore del numero di carte

Il sistema SHALL mostrare il numero di carte visibili **solo quando è attivo un filtro** — testuale, per label, o entrambi — nella forma "trovate/totale" (es. "5/30"). A riposo (nessuna ricerca testuale e nessuna label selezionata) l'indicatore NON MUST essere mostrato, per non occupare spazio con un'informazione che non serve a chi sta semplicemente guardando le proprie carte.

#### Scenario: Nessun indicatore a riposo

- **WHEN** il campo di ricerca è vuoto e nessun chip è selezionato
- **THEN** l'indicatore del conteggio non è mostrato

#### Scenario: Conteggio durante il filtro testuale

- **WHEN** l'utente ha digitato un testo di ricerca
- **THEN** l'indicatore mostra il numero di carte trovate sul totale (es. "5/30")

#### Scenario: Conteggio con filtro per label

- **WHEN** l'utente ha selezionato una o più label, con o senza testo di ricerca
- **THEN** l'indicatore mostra il numero di carte visibili sul totale delle carte salvate

#### Scenario: Indicatore che scompare al ripristino

- **WHEN** l'utente svuota la ricerca e deseleziona tutti i chip
- **THEN** l'indicatore smette di essere mostrato e lo spazio torna alla griglia
