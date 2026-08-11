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

		// Rotte della sezione Scontrini (la lista è una ShellContent, non una rotta).
		Routing.RegisterRoute("ReceiptFormPage", typeof(ReceiptFormPage));
		Routing.RegisterRoute("ReceiptDetailPage", typeof(ReceiptDetailPage));

		// Rotta della pagina Impostazioni.
		Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));

		// Rotta della sezione Backup su Google Drive (raggiungibile dalle Impostazioni).
		Routing.RegisterRoute("BackupPage", typeof(BackupPage));

		// Rotta della sezione Controllo aggiornamenti (raggiungibile dalle Impostazioni).
		Routing.RegisterRoute("UpdatePage", typeof(UpdatePage));
	}
}
