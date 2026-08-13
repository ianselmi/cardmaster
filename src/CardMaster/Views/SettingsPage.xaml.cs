using CardMaster.ViewModels;

namespace CardMaster.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Al ritorno dalla pagina Backup lo stato può essere cambiato (abilitato, disabilitato,
        // riconnesso, o un backup appena riuscito che ha spento il segnale di problema).
        _viewModel.RefreshBackupState();
        _viewModel.RefreshAiSection();
    }

    /// <summary>
    /// Accendere la funzione è un'azione esplicita e informata: si dichiara che cosa uscirà dal
    /// device, verso chi e a spese di chi, e se l'utente rifiuta l'interruttore torna indietro.
    /// Spegnerla non chiede niente — smettere di inviare non ha bisogno di consenso.
    /// </summary>
    private async void OnAiScanToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch toggle || !e.Value)
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Attivare la lettura assistita?",
            _viewModel.AiDisclosureText,
            "Attiva",
            "Annulla");

        if (!confirmed)
        {
            // Riporta indietro l'interruttore: il binding ha già scritto la preferenza.
            toggle.IsToggled = false;
        }
    }

    private async void OnSetAiKeyClicked(object? sender, EventArgs e)
    {
        // La chiave si incolla, non si rilegge: qui non si mostra mai quella già configurata.
        var key = await DisplayPromptAsync(
            "Chiave API",
            "Incolla la tua chiave di Anthropic. Resta nell'archivio protetto del telefono e non " +
            "sarà più visibile da qui.",
            accept: "Salva",
            cancel: "Annulla",
            placeholder: "sk-ant-...",
            maxLength: 200);

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var saved = await _viewModel.SetAiKeyAsync(key);
        await DisplayAlertAsync(
            saved ? "Chiave salvata" : "Chiave non salvata",
            saved
                ? "Puoi provarla subito con «Verifica la chiave»."
                : "Non è stato possibile scriverla nell'archivio protetto del telefono.",
            "OK");
    }

    private async void OnVerifyAiKeyClicked(object? sender, EventArgs e)
    {
        var message = await _viewModel.VerifyAiKeyAsync();
        await DisplayAlertAsync("Verifica della chiave", message, "OK");
    }

    private async void OnRemoveAiKeyClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Rimuovere la chiave?",
            "La lettura assistita smetterà di funzionare. Gli scontrini già salvati non vengono toccati.",
            "Rimuovi",
            "Annulla");

        if (!confirmed)
        {
            return;
        }

        await _viewModel.RemoveAiKeyAsync();
    }

    private async void OnBackupClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("BackupPage");
    }

    private async void OnUpdateClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("UpdatePage");
    }

    private async void OnClearReceiptImagesClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Eliminare le immagini?",
            "Gli scontrini restano, con i dati e il testo riconosciuto: si perde solo la foto originale.",
            "Elimina",
            "Annulla");

        if (!confirmed)
        {
            return;
        }

        await _viewModel.ClearReceiptImagesAsync();
    }
}
