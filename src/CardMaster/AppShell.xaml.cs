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
		Routing.RegisterRoute("EditCardPage", typeof(EditCardPage));
		Routing.RegisterRoute("SharePage", typeof(SharePage));

		// Rotta della pagina Impostazioni.
		Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));

		// Rotta della sezione Backup su Google Drive (raggiungibile dalle Impostazioni).
		Routing.RegisterRoute("BackupPage", typeof(BackupPage));
	}
}
