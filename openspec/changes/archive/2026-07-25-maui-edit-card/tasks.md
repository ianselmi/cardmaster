## 1. ViewModel di modifica

- [x] 1.1 Creare `EditCardViewModel` (in `ViewModels/`) con `IQueryAttributable` che riceve l'`id` della carta
- [x] 1.2 Implementare `LoadAsync`: caricare la carta via `ICardRepository.GetByIdAsync`; se assente/tombstone impostare un flag `CardExists = false` (la pagina torna indietro)
- [x] 1.3 Esporre le proprietà editabili: `DisplayName`, selezione emittente (opzioni catalogo/`Nessuno`/`Altro…`, `CustomIssuerName`, `IsCustomIssuer`), `SelectedFormat`; e il `BarcodeValue` in sola lettura
- [x] 1.4 Riusare la logica di selezione/arricchimento emittente dal catalogo (`IIssuerCatalog`): al cambio emittente ereditare colore/logo/formato atteso quando presenti, senza sovrascrivere campi già valorizzati (come in `AddCardViewModel`)
- [x] 1.5 Pre-selezionare in `LoadAsync` l'opzione emittente corrente della carta (catalogo, libero via `Altro…`, o `Nessuno`) e il formato corrente
- [x] 1.6 Implementare `Validate(out string error)`: nome visualizzato e formato obbligatori (barcode non validato, immutabile)
- [x] 1.7 Implementare `SaveAsync`: aggiornare l'istanza caricata (preservando `Id`/`CreatedAt`/`Barcode`) e chiamare `ICardRepository.UpdateAsync` (rinnova `UpdatedAt`)
- [x] 1.8 Implementare `DeleteAsync`: chiamare `ICardRepository.SoftDeleteAsync(id)` (tombstone)

## 2. Pagina di modifica

- [x] 2.1 Creare `EditCardPage` (XAML + code-behind) con form: nome, picker emittente (+ campo emittente libero quando `Altro…`), picker formato, e valore barcode in sola lettura
- [x] 2.2 In `OnAppearing`/code-behind: chiamare `InitializeAsync` (catalogo) e `LoadAsync`; se `CardExists == false` tornare indietro senza errore
- [x] 2.3 Pulsante "Salva": eseguire `Validate`, mostrare il messaggio in caso di errore, altrimenti `SaveAsync` e navigare indietro alla carta
- [x] 2.4 Registrare la rotta `EditCardPage` in `AppShell` e la pagina/ViewModel nel container DI in `MauiProgram`

## 3. Ingresso da visualizzazione ed eliminazione

- [x] 3.1 Aggiungere a `ShowCardPage` i `ToolbarItem` "Modifica" ed "Elimina"
- [x] 3.2 "Modifica": navigare a `EditCardPage` passando l'`id` della carta corrente
- [x] 3.3 "Elimina": `DisplayAlert` di conferma (Elimina/Annulla); se confermato chiamare `DeleteAsync` (o `ICardRepository.SoftDeleteAsync`) e tornare alla lista
- [x] 3.4 Far sì che `ShowCardPage` ricarichi i dati al ritorno dalla modifica (reset del guard `_loaded` / metodo di reload in `OnAppearing` o `OnNavigatedTo`) così la visualizzazione riflette i nuovi valori

## 4. Verifica

- [x] 4.1 Verificare i comportamenti chiave: modifica nome/emittente/formato persistita sullo stesso `Id`; barcode invariato; eliminazione con conferma rimuove la carta dalla lista; annulla non modifica nulla
- [x] 4.2 `dotnet build` con **0 errori** (criterio di accettazione obbligatorio)
