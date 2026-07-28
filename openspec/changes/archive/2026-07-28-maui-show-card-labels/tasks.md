## 1. ViewModel

- [x] 1.1 In `src/CardMaster/ViewModels/ShowCardViewModel.cs` esporre le label della carta come collezione di sola lettura (proprietà osservabile) e un flag `HasLabels` per la visibilità della sezione
- [x] 1.2 Popolare entrambe dentro `LoadAsync()`, dalla carta già caricata (`card.Labels`), accanto agli altri campi proiettati — nessuna query aggiuntiva al repository
- [x] 1.3 Verificare che il caso "carta non trovata" (`CardExists = false`) lasci la collezione vuota e `HasLabels` falso

## 2. Pagina di dettaglio

- [x] 2.1 In `src/CardMaster/Views/ShowCardPage.xaml` aggiungere, **tra** l'area barcode e la `Grid` dei pulsanti, un `FlexLayout` con `Wrap="Wrap"` legato alle label, con `IsVisible="{Binding HasLabels}"`
- [x] 2.2 Rendere i chip come nell'editor label (`Border` pieno `Secondary`, `CornerRadius` 16, testo `PrimaryDarkText`, `FontSizeBody`, margine `0,0,8,8`), **senza** la ✕ e senza gesture di rimozione
- [x] 2.3 Non aggiungere intestazioni o etichette di sezione sopra i chip
- [x] 2.4 Verificare che nessuna modifica sia necessaria in `ShowCardPage.xaml.cs`: il refresh dopo la modifica è già garantito da `ReloadAsync()` in `OnAppearing`

## 3. Verifica

- [x] 3.1 `dotnet build` sulla soluzione: 0 errori (criterio di accettazione obbligatorio)
- [x] 3.2 Verifica su emulatore (skill `android-emulator`): una carta **con** label mostra tutti i chip nel dettaglio
- [x] 3.3 Verifica su emulatore: una carta **senza** label non mostra né chip né spazio vuoto aggiuntivo, e l'area barcode è nella stessa posizione dei due casi
- [x] 3.4 Verifica su emulatore: toccare un chip nel dettaglio non rimuove né modifica nulla
- [x] 3.5 Verifica su emulatore: modificando le label da Modifica e tornando indietro, il dettaglio mostra subito le label aggiornate
- [x] 3.6 Verifica su emulatore: una carta con abbastanza label da non stare su una riga le manda a capo, tutte leggibili, col barcode invariato
- [x] 3.7 Verifica su emulatore in tema chiaro e scuro: chip leggibili a contrasto

## 4. Documentazione

- [x] 4.1 Aggiungere la voce `maui-show-card-labels` all'elenco delle change v1 in `PLAN.md`, con esito della verifica e data
- [x] 4.2 Rivedere il diff prima del commit (repository pubblico): nessun segreto, token o dato personale
