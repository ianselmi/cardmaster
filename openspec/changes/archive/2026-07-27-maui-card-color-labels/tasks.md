## 1. Modello dati e persistenza

- [x] 1.1 Aggiungere a `Data/Card.cs` il campo `TileColor` (hex, nullable — colore scelto dall'utente; `null` = automatico) e la colonna `LabelsCsv` (nullable), con commenti XML che distinguono `TileColor` dal `Color` di brand ereditato dall'emittente
- [x] 1.2 Aggiungere a `Card` la proprietà `[Ignore] Labels` (lista tipizzata) che legge/scrive `LabelsCsv` tramite l'helper del passo 1.3
- [x] 1.3 Creare `Data/CardLabels.cs`: `Parse`/`Serialize` (separatore `|`), `Normalize` (trim, collasso spazi, rimozione controlli e separatore, troncamento a 24 caratteri) e `Merge` con deduplicazione case/accent-insensitive che conserva la prima grafia e rispetta il massimo di 8 label
- [x] 1.4 Estrarre la normalizzazione case/accent-insensitive oggi privata in `CardListViewModel` in un helper condiviso (es. `Services/TextNormalizer.cs`) e usarlo sia nella ricerca sia nel confronto tra label
- [x] 1.5 Portare `DatabaseService.SchemaVersion` a 2 e verificare che `CreateTableAsync<Card>()` aggiunga le colonne su un db esistente senza perdere dati
- [x] 1.6 Verificare che `BackupNaming.CanRestore` continui ad accettare i backup `v1` esistenti e a rifiutare un `v2` su app più vecchia (nessuna modifica attesa al codice del backup)

## 2. Colore del riquadro

- [x] 2.1 In `Views/CardTilePalette.cs` esporre la palette come `IReadOnlyList<Color>` e aggiungere `ForCard(Card)` che restituisce `TileColor` quando valorizzato, altrimenti `ForName(DisplayName)`
- [x] 2.2 Sostituire `NameToTileColorConverter` con un converter che riceve la carta intera e delega a `ForCard`; aggiornare i due `DataTemplate` di `CardListPage.xaml` (griglia e barra "usate di recente")
- [x] 2.3 Creare la vista riusabile del selettore colore: pastiglie della palette con evidenza della selezione, più la pastiglia "Automatico" che mostra in anteprima il colore derivato dal nome corrente

## 3. Editor delle label (vista condivisa)

- [x] 3.1 Creare la vista riusabile dell'editor label: `Entry` + pulsante "Aggiungi" (invio da tastiera equivalente), chip delle label assegnate con ✕ per rimuoverle, riga dei suggerimenti
- [x] 3.2 Definire la forma di API comune ai due ViewModel (`Labels`, `LabelSuggestions`, `AddLabel`, `RemoveLabel`, `NewLabelText`) e la logica di aggiunta che applica normalizzazione, dedup e limite, segnalando il limite raggiunto
- [x] 3.3 Popolare i suggerimenti dalle label delle carte attive (via `ICardRepository.GetAllAsync`), escludendo quelle già assegnate alla carta corrente, ordinate alfabeticamente

## 4. Schermata di modifica

- [x] 4.1 `EditCardViewModel`: caricare `TileColor` e `Labels` dalla carta, esporre selezione colore e API label del passo 3.2
- [x] 4.2 `EditCardViewModel.SaveAsync`: persistere `TileColor` e `Labels` nella stessa `UpdateAsync`, senza toccare `Color`, `Id`, `CreatedAt` e `Barcode`
- [x] 4.3 Verificare che `ApplyIssuerSelection` (arricchimento dal catalogo) non sovrascriva `TileColor` né le label
- [x] 4.4 `Views/EditCardPage.xaml`: inserire selettore colore ed editor label sotto il nome della carta

## 5. Schermata di creazione

- [x] 5.1 `AddCardViewModel`: esporre gli stessi campi (colore "Automatico" e nessuna label come default) e valorizzare `TileColor`/`Labels` sulla `Card` creata in `SaveAsync`
- [x] 5.2 Verificare che il percorso "carta ricevuta via QR" resti invariato (nessuna label nel payload; il colore di brand ricevuto continua a finire in `Color`)
- [x] 5.3 `Views/AddCardPage.xaml`: inserire le due sezioni senza rendere obbligatorio nulla e senza spostare i campi esistenti

## 6. Filtro per label nella lista

- [x] 6.1 `CardListViewModel`: esporre `LabelFilters` (`ObservableCollection<LabelFilterItem>` con `Name`/`IsSelected`) e `HasLabelFilters`, ricostruita in `LoadAsync` dalle label delle carte attive
- [x] 6.2 Preservare le selezioni attive attraverso il ricaricamento e potare quelle rimaste orfane
- [x] 6.3 Estendere `ApplyFilter` con la condizione OR sulle label selezionate, in AND con il filtro testuale esistente; verificare che `CountText` continui a riflettere il risultato
- [x] 6.4 Aggiornare il testo dello stato vuoto quando il vuoto dipende da un filtro per label attivo
- [x] 6.5 `Views/CardListPage.xaml`: riga di chip orizzontale sopra la griglia (sotto il conteggio), nascosta quando non ci sono label; stile del chip selezionato/non selezionato coerente con la palette del restyle

## 7. Verifica

- [x] 7.1 `dotnet build` con 0 errori
- [x] 7.2 Verifica su emulatore: carte esistenti invariate all'aggiornamento (colori identici, nessun chip visibile)
- [x] 7.3 Verifica su emulatore: scelta colore da palette e ritorno ad "Automatico", su carta nuova e su carta esistente
- [x] 7.4 Verifica su emulatore: aggiunta/rimozione label, dedup con maiuscole e accenti diversi, limite di 8 label, suggerimenti
- [x] 7.5 Verifica su emulatore: filtro a chip singolo e multiplo (OR), combinazione con la ricerca, conteggio, stato vuoto, selezione orfana dopo aver tolto l'ultima label
- [x] 7.6 Rivedere il diff prima del commit (repository pubblico: nessun segreto o dato personale) e aggiornare `PLAN.md` con la voce della change
