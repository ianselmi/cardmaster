namespace CardMaster.Views;

/// <summary>
/// Editor delle label di una carta (composizione, chip rimovibili, suggerimenti),
/// condiviso da creazione e modifica. Il BindingContext è quello della pagina
/// ospitante (un <c>CardFormViewModel</c>).
/// </summary>
public partial class CardLabelEditorView : ContentView
{
    public CardLabelEditorView()
    {
        InitializeComponent();
    }
}
