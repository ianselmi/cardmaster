using CardMaster.Views;

namespace CardMaster;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Rotte per la navigazione di acquisizione carta.
		Routing.RegisterRoute("ScanPage", typeof(ScanPage));
		Routing.RegisterRoute("AddCardPage", typeof(AddCardPage));
		Routing.RegisterRoute("ShowCardPage", typeof(ShowCardPage));
	}
}
