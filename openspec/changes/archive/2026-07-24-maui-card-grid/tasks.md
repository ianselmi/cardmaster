## 1. Palette e colore per carta

- [x] 1.1 Creare `CardTilePalette` (palette curata di colori leggibili con testo bianco) e un hash deterministico stabile del nome (es. FNV-1a) → indice colore
- [x] 1.2 Creare `NameToTileColorConverter` (`IValueConverter`) che restituisce il `Color` del tile dal `DisplayName`

## 2. Layout a griglia

- [x] 2.1 In `CardListPage.xaml` sostituire il layout a lista con `CollectionView` + `GridItemsLayout(Span=2, Orientation=Vertical)` e spaziatura
- [x] 2.2 Item template: `Border` con angoli arrotondati (`RoundRectangle`), sfondo dal converter, forma tendenzialmente quadrata (HeightRequest)
- [x] 2.3 Contenuto tile: nome (evidenza) + emittente (secondario), testo bianco a contrasto; gestire il caso senza emittente
- [x] 2.4 Mantenere l'empty state esistente ("Nessuna carta ancora")
- [x] 2.5 Registrare il converter come risorsa (pagina o app)

## 3. Verifica

- [x] 3.1 `dotnet build`: compila senza errori (criterio di accettazione)
- [x] 3.2 Verifica runtime su emulatore: con alcune carte, la lista appare come griglia a 2 colonne di riquadri arrotondati colorati; stesso nome → stesso colore; empty state invariato (screenshot) — *verificato: 2 tile affiancati, angoli arrotondati, colori distinti per nome (teal/viola), nome bold + emittente secondario, testo bianco*
- [x] 3.3 `openspec validate maui-card-grid` senza errori
