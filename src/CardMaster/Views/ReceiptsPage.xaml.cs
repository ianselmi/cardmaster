using CardMaster.Services.Receipts;
using CardMaster.ViewModels;

namespace CardMaster.Views;

/// <summary>
/// Sezione Scontrini: acquisizione, storico e spesa del mese.
/// L'acquisizione vive qui perché è il punto d'ingresso naturale della sezione.
/// </summary>
public partial class ReceiptsPage : ContentPage
{
    private readonly ReceiptListViewModel _viewModel;
    private readonly IReceiptOcr _ocr;

    public ReceiptsPage(ReceiptListViewModel viewModel, IReceiptOcr ocr)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _ocr = ocr;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnCapturePhotoClicked(object? sender, EventArgs e)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await DisplayAlertAsync(
                "Fotocamera non disponibile",
                "Questo dispositivo non permette di scattare foto. Puoi comunque scegliere un'immagine già salvata.",
                "OK");
            return;
        }

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync(
                "Permesso fotocamera negato",
                "Serve la fotocamera per fotografare lo scontrino. Puoi comunque usare \"Da immagine\" e scegliere una foto già sul telefono.",
                "OK");
            return;
        }

        FileResult? photo;
        try
        {
            photo = await MediaPicker.Default.CapturePhotoAsync();
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Impossibile scattare la foto", "Riprova, oppure scegli un'immagine già salvata.", "OK");
            return;
        }

        // Annullato: si resta sulla pagina, senza messaggi e senza creare nulla.
        if (photo is null)
        {
            return;
        }

        await AnalyzeAndConfirmAsync(photo.FullPath);
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        FileResult? file;
        try
        {
            // FilePicker (SAF): il selettore di sistema non richiede permessi di storage.
            file = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Scegli l'immagine dello scontrino",
                FileTypes = FilePickerFileType.Images,
            });
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Impossibile aprire il selettore", "Non è stato possibile aprire il selettore di immagini.", "OK");
            return;
        }

        if (file is null)
        {
            return;
        }

        await AnalyzeAndConfirmAsync(file.FullPath);
    }

    /// <summary>
    /// Riconosce il testo, ne estrae la testata e apre la conferma. Il riconoscimento è
    /// interamente sul device: l'immagine non lascia il telefono.
    /// </summary>
    private async Task AnalyzeAndConfirmAsync(string imagePath)
    {
        CaptureButton.IsEnabled = false;
        PickImageButton.IsEnabled = false;

        try
        {
            var result = await _ocr.RecognizeAsync(imagePath);
            if (result.IsEmpty)
            {
                await DisplayAlertAsync(
                    "Nessun testo riconosciuto",
                    "In questa immagine non è stato trovato testo leggibile. Riprova con una foto più dritta e a fuoco, con lo scontrino ben illuminato.",
                    "OK");
                return;
            }

            // Si passa (e si conserva) il testo con le righe ricostruite dalla geometria, non
            // quello grezzo di ML Kit: sul grezzo le colonne sono separate e la riga del totale
            // arriva senza importo. Così il testo è anche leggibile nel dettaglio e ri-parsabile.
            // Le righe si ricostruiscono qui, dove la geometria dell'OCR è ancora disponibile:
            // il testo conservato non porta le coordinate, e senza coordinate le colonne non si
            // separano più. È la ragione per cui questa struttura viaggia fino alla conferma
            // invece di essere ricalcolata lì come la testata.
            var layout = ReceiptTextLayout.ToVisualLayout(result);

            await Shell.Current.GoToAsync("ReceiptFormPage", new Dictionary<string, object>
            {
                ["rawText"] = string.Join("\n", layout.Select(l => l.Text)),
                ["imagePath"] = imagePath,
                ["items"] = ReceiptItemsParser.Parse(layout),
            });
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Immagine non leggibile",
                "Non è stato possibile analizzare questa immagine. Prova con un'altra.",
                "OK");
        }
        finally
        {
            CaptureButton.IsEnabled = true;
            PickImageButton.IsEnabled = true;
        }
    }

    private async void OnReceiptSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ReceiptListItem item)
        {
            return;
        }

        // Deseleziona subito: tornando indietro la riga non deve restare evidenziata.
        if (sender is CollectionView collection)
        {
            collection.SelectedItem = null;
        }

        await Shell.Current.GoToAsync("ReceiptDetailPage", new Dictionary<string, object>
        {
            ["id"] = item.Id,
        });
    }
}
