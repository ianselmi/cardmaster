# visual-identity

## Purpose

Identità visiva dell'app CardMaster: logo (app icon + splash), palette di brand (primario/accent, superfici, testo) come unica sorgente di verità, scala tipografica e di spaziatura condivisa, e coerenza tra tema chiaro e scuro. Copre gli asset e le risorse di stile, distinta dalla logica delle singole pagine.

## Requirements

### Requirement: Logo dell'app (icon + splash)

Il sistema SHALL presentare un logo proprio di CardMaster — una tessera arrotondata con barre di barcode stilizzate — come **app icon** (background + foreground) e come **splash screen**, in sostituzione del segnaposto "NET" del template MAUI. Il logo MUST essere fornito come asset **SVG** vettoriale, leggibile alle dimensioni tipiche dell'icona di lancio Android e riconoscibile sia in tema chiaro sia scuro.

#### Scenario: Icona di lancio personalizzata
- **WHEN** l'app è installata su un device Android e l'utente guarda la home/app drawer
- **THEN** l'icona mostra il logo tessera+barcode di CardMaster (non le lettere "NET" né il viola del template)

#### Scenario: Splash all'avvio
- **WHEN** l'app viene avviata
- **THEN** lo splash screen mostra il logo di CardMaster sul colore di brand, poi apre la lista carte

### Requirement: Palette di brand come sorgente di verità

Il sistema SHALL definire un colore **primario/accent di brand** (famiglia ambra/arancio caldo) e i colori di superficie e testo associati, dichiarati in un'unica sorgente di verità delle risorse di stile e riferiti ovunque servano (risorse MAUI, colori nativi Android, colore di icona/splash). I riferimenti al viola `#512BD4` del template MUST essere rimossi. Il colore primario MUST garantire un contrasto sufficiente con il testo che vi si sovrappone.

#### Scenario: Nessun residuo del template
- **WHEN** si ispezionano le risorse colore dell'app (risorse MAUI, colori Android, config icona/splash)
- **THEN** il colore primario è quello di brand ambra/arancio e non compare più il viola `#512BD4` del template

#### Scenario: Accent applicato ai controlli
- **WHEN** l'utente vede elementi interattivi con colore d'accento (es. pulsante Salva, barra di navigazione, indicatori)
- **THEN** questi usano il colore di brand, con testo leggibile a contrasto

### Requirement: Colori dei riquadri allineati al brand

Il sistema SHALL derivare i colori di sfondo dei riquadri della lista carte da una palette **coerente con l'identità di brand**, mantenendo la generazione **deterministica** per nome carta e la leggibilità del testo a contrasto già richieste da `card-list`. La palette dei riquadri MUST restare distinta e sufficientemente varia da distinguere le carte, senza confondersi con il colore d'accento dei controlli.

#### Scenario: Riquadri coerenti col brand
- **WHEN** la lista carte mostra più carte con nomi diversi
- **THEN** i riquadri usano colori della palette di brand, deterministici per nome, con testo leggibile a contrasto

### Requirement: Scala tipografica e di spaziatura coerente

Il sistema SHALL definire una scala tipografica (almeno: titolo, corpo, caption) e valori di spaziatura/padding standard come risorse condivise, e SHALL applicarli in modo uniforme alle pagine esistenti (lista carte, dettaglio carta, aggiungi/scansiona carta) senza alterarne la funzione o il layout logico.

#### Scenario: Uniformità tra le pagine
- **WHEN** l'utente naviga tra lista, dettaglio e aggiunta carta
- **THEN** dimensioni dei testi, spaziature e padding seguono la stessa scala condivisa, senza incoerenze evidenti tra una pagina e l'altra

### Requirement: Coerenza tema chiaro e scuro

Il sistema SHALL garantire che la nuova palette e gli stili restino leggibili e coerenti sia in tema chiaro sia in tema scuro. L'area di rendering del barcode MUST restare sempre su fondo bianco anche in tema scuro, come già richiesto da `card-display`.

#### Scenario: Leggibilità in dark mode
- **WHEN** il device è in tema scuro e l'utente apre lista, dettaglio e aggiunta carta
- **THEN** testi e superfici restano leggibili con contrasto adeguato, e il barcode resta nero su fondo bianco
