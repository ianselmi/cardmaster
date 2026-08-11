using CardMaster.Services.Receipts;
using CardMaster.ViewModels;

namespace CardMaster.Views;

/// <summary>
/// Conferma e correzione dei dati di testata. Nessuno scontrino viene salvato senza passare
/// di qui: il riconoscimento propone, l'utente decide.
/// </summary>
public partial class ReceiptFormPage : ContentPage, IQueryAttributable
{
    private readonly ReceiptFormViewModel _viewModel;

    public ReceiptFormPage(ReceiptFormViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Modifica di uno scontrino già salvato.
        if (query.TryGetValue("id", out var id) && id is string receiptId)
        {
            Title = "Modifica scontrino";
            if (!await _viewModel.LoadExistingAsync(receiptId))
            {
                await Shell.Current.GoToAsync("..");
            }

            return;
        }

        // Scontrino appena acquisito: si ri-estrae la testata dal testo riconosciuto.
        // Il parser è puro e istantaneo, quindi rifarlo qui costa meno che trasportare
        // una struttura di stato tra le pagine.
        var rawText = query.TryGetValue("rawText", out var raw) && raw is string text ? text : string.Empty;
        var imagePath = query.TryGetValue("imagePath", out var path) && path is string p ? p : null;

        _viewModel.InitializeFromCapture(ReceiptHeaderParser.Parse(rawText), rawText, imagePath);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.Validate(out var error))
        {
            await DisplayAlertAsync("Dato non valido", error, "OK");
            return;
        }

        await _viewModel.SaveAsync();
        await Shell.Current.GoToAsync("..");
    }
}
