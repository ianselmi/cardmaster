using CardMaster.ViewModels;

namespace CardMaster.Views;

/// <summary>Dettaglio di uno scontrino: dati, immagine se conservata, testo riconosciuto.</summary>
public partial class ReceiptDetailPage : ContentPage, IQueryAttributable
{
    private readonly ReceiptDetailViewModel _viewModel;
    private string? _receiptId;

    public ReceiptDetailPage(ReceiptDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is string receiptId)
        {
            _receiptId = receiptId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Ricarica a ogni comparsa: tornando dalla modifica i dati devono essere quelli nuovi.
        if (_receiptId is null || !await _viewModel.LoadAsync(_receiptId))
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (_receiptId is null)
        {
            return;
        }

        await Shell.Current.GoToAsync("ReceiptFormPage", new Dictionary<string, object>
        {
            ["id"] = _receiptId,
        });
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Eliminare lo scontrino?",
            "Sparisce dallo storico e dai totali di spesa. L'immagine viene rimossa dal telefono.",
            "Elimina",
            "Annulla");

        if (!confirmed)
        {
            return;
        }

        await _viewModel.DeleteAsync();
        await Shell.Current.GoToAsync("..");
    }
}
