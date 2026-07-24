## Why

L'utente crea e vede le carte in griglia, ma non può ancora **usarle alla cassa**. Serve aprire una carta e mostrarne il barcode a schermo intero, ben leggibile dal lettore del cassiere, con luminosità al massimo e schermo che non si spegne, e il codice in chiaro come rete di sicurezza.

## What Changes

- Tap su un riquadro della lista → nuova **pagina di visualizzazione carta** (`ShowCardPage`).
- **Rendering del barcode** con `ZXing.Net` + `ZXing.Net.Bindings.SkiaSharp`: dal valore/formato della carta si genera un'immagine nero-su-bianco mostrata grande e centrata.
- **Sfondo bianco** dell'area barcode **anche in dark mode** (contrasto per i lettori).
- **Codice in chiaro sempre visibile** sotto il barcode (fallback per il cassiere).
- **Robustezza**: se il valore non è generabile nel formato scelto (alla cattura non si valida il checksum), niente crash — si mostra comunque il codice in chiaro + un messaggio ("mostra il numero al cassiere").
- **Schermo sveglio** mentre la carta è aperta (`DeviceDisplay.KeepScreenOn`), ripristinato all'uscita.
- **Luminosità al massimo** all'apertura (codice Android), **ripristinata al default di sistema** all'uscita.
- **Rilevamento best-effort del filtro luce blu** (Night Light): se attivo, avviso non bloccante che suggerisce di disattivarlo per una lettura migliore. Se non rilevabile sull'OEM, nessun avviso.

## Capabilities

### New Capabilities
- `card-display`: apertura di una carta e visualizzazione del suo barcode a schermo intero (rendering, codice in chiaro, luminosità/keep-awake, avviso filtro luce blu), ottimizzata per la lettura alla cassa.

### Modified Capabilities
- Nessuna. (Il tap sui riquadri di `card-list` diventa una rotta verso `card-display`; è un aggancio di navigazione, non un cambio di requisito della lista.)

## Impact

- **Nuovo codice**: `ShowCardPage` + `ShowCardViewModel`; servizio/helper di rendering barcode (ZXing→SkiaSharp→`ImageSource`); mappatura `BarcodeFormat`→enum ZXing; helper di piattaforma Android per luminosità e rilevamento Night Light; navigazione dal tile.
- **Dipendenze (NuGet)**: `ZXing.Net`, `ZXing.Net.Bindings.SkiaSharp` (che porta `SkiaSharp`).
- **Nessuna rete**: tutto locale e offline.
- **Vincolo di qualità**: la soluzione deve **compilare senza errori** (`dotnet build`), criterio di accettazione.
- **Fuori scope**: modifica/eliminazione carta, condivisione via QR (`maui-share-qr`), rotazione landscape, disattivazione automatica del filtro luce blu (non possibile senza permesso privilegiato).
- **Change successive abilitate**: `maui-share-qr` (condivisione della carta mostrata).
