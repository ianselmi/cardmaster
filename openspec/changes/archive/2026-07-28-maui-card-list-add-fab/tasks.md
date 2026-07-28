## 1. Risorse di stile

- [x] 1.1 In `src/CardMaster/Resources/Styles/Styles.xaml`, accanto alle altezze della testata della lista, aggiungere le risorse del bottone flottante: lato (`56`), raggio d'angolo (`28`), margine dal fondo e `FontSize` del glifo `+`, con un commento che le lega a questa change come per `FilterChipRowHeight`
- [x] 1.2 Aggiungere la risorsa di spazio riservato in fondo alla griglia carte, derivata da lato + margine del bottone, così che l'ultima riga si possa scorrere sopra al bottone

## 2. Lista carte

- [x] 2.1 In `src/CardMaster/Views/CardListPage.xaml` rimuovere il `ToolbarItem Text="Aggiungi"`, lasciando la sola voce Impostazioni con il suo `IconImageSource` bindato al badge di aggiornamento
- [x] 2.2 Riservare in fondo alla `CollectionView` della griglia lo spazio della risorsa 1.2, mantenendo il margine superiore attuale (via `CollectionView.Footer` con `BoxView` trasparente: `ItemsView` non espone `Padding`, vedi `design.md`)
- [x] 2.3 Aggiungere come secondo figlio di `Grid.Row="1"`, dopo la `CollectionView`, il `Button` tondo: `Text="+"`, lato/raggio/`FontSize` dalle risorse, `Padding="0"`, `HorizontalOptions="Center"`, `VerticalOptions="End"`, margine dal fondo dalla risorsa, `Clicked="OnAddClicked"`
- [x] 2.4 Impostare sul bottone `SemanticProperties.Description="Aggiungi carta"` e una `Shadow` leggera
- [x] 2.5 Verificare che `OnAddClicked` in `src/CardMaster/Views/CardListPage.xaml.cs` sia invariato e ancora agganciato (nessuna modifica al code-behind attesa oltre a questo controllo)

## 3. Verifica

- [x] 3.1 `dotnet build` sulla soluzione: 0 errori (criterio di accettazione obbligatorio)
- [x] 3.2 Verifica su emulatore (skill `android-emulator`): il bottone è un **cerchio** in basso al centro con il `+` centrato anche in verticale; se decentrato applicare la correzione prevista in `design.md`
- [x] 3.3 Verifica su emulatore: il tocco apre la schermata di scansione; il bottone resta fermo durante lo scorrimento; l'ultima riga di carte è raggiungibile e non coperta; il bottone è presente anche a lista vuota e con filtro a zero risultati
- [x] 3.4 Verifica su emulatore in tema chiaro e scuro: `+` leggibile a contrasto e ombra senza artefatti sull'angolo tondo (se resa male, rimuovere la `Shadow` come previsto)
- [x] 3.5 Confermare che la toolbar non mostra più "Aggiungi" e che Impostazioni + badge di aggiornamento funzionano come prima

## 4. Documentazione

- [x] 4.1 Aggiungere la voce `maui-card-list-add-fab` all'elenco delle change v1 in `PLAN.md`, con esito della verifica e data
- [x] 4.2 Rivedere il diff prima del commit (repository pubblico): nessun segreto, token o dato personale
