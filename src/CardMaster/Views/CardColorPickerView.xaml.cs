namespace CardMaster.Views;

/// <summary>
/// Selettore del colore del riquadro, condiviso da creazione e modifica carta.
/// Il BindingContext è quello della pagina ospitante (un <c>CardFormViewModel</c>).
/// </summary>
public partial class CardColorPickerView : ContentView
{
    public CardColorPickerView()
    {
        InitializeComponent();
    }
}
