## 1. ViewModel

- [x] 1.1 In `src/CardMaster/ViewModels/ShowCardViewModel.cs` esporre una proprietà osservabile `Color TileColor`, inizializzata a `CardTilePalette.ForCard(null)`
- [x] 1.2 Valorizzarla in `LoadAsync()` con `CardTilePalette.ForCard(card)`, accanto agli altri campi proiettati — senza duplicare la regola "colore utente altrimenti derivato dal nome"
- [x] 1.3 Nel ramo "carta non trovata" riportarla a `CardTilePalette.ForCard(null)`, come già si fa per le label

## 2. Pagina di dettaglio

- [x] 2.1 In `src/CardMaster/Views/ShowCardPage.xaml` aggiungere come **primo figlio** del `VerticalStackLayout` dentro il `Border` bianco una fascia (`Border` con `CornerRadius`) legata a `TileColor`, a tutta larghezza
- [x] 2.2 Dentro la fascia il `Label` col nome: `Text="{Binding DisplayName}"`, testo `White` in grassetto, centrato, con `MaxLines` per andare a capo sui nomi lunghi senza divorare il riquadro
- [x] 2.3 Verificare che immagine barcode, messaggio di fallback e codice in chiaro restino invariati e su fondo bianco
- [x] 2.4 Verificare che nessuna modifica sia necessaria in `ShowCardPage.xaml.cs`

## 3. Verifica

- [x] 3.1 `dotnet build` sulla soluzione: 0 errori (criterio di accettazione obbligatorio)
- [x] 3.2 Verifica su emulatore (skill `android-emulator`): il nome compare dentro il riquadro, sopra il barcode, su fascia colorata
- [x] 3.3 Verifica su emulatore: una carta **con colore scelto dall'utente** mostra la fascia di quel colore; una **senza** mostra il colore derivato dal nome, lo stesso del riquadro nella griglia
- [x] 3.4 Verifica su emulatore in **tema scuro**: area barcode e codice in chiaro restano su fondo bianco, solo la fascia è colorata
- [x] 3.5 Verifica su emulatore: nome lungo (più righe) leggibile, riquadro non deformato, barcode non coperto
- [x] 3.6 Verifica su emulatore: la barra del titolo mostra ancora il nome

## 4. Documentazione

- [x] 4.1 Aggiungere la voce `maui-show-card-name-badge` all'elenco delle change v1 in `PLAN.md`, con esito della verifica e data
- [x] 4.2 Rivedere il diff prima del commit (repository pubblico): nessun segreto, token o dato personale
