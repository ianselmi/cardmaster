using BarcodeScanning;
using CardMaster.Services;
using CardMaster.Services.Backup;
using CardMaster.Services.Update;
using CardMaster.ViewModels;
using CardMaster.Views;
using Microsoft.Extensions.Logging;

namespace CardMaster;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// Attiva il provider SQLCipher (unico bundle referenziato: e_sqlcipher).
		// Necessario perché PRAGMA key cifri davvero il database.
		SQLitePCL.Batteries_V2.Init();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseBarcodeScanning()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		RegisterServices(builder.Services);

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void RegisterServices(IServiceCollection services)
	{
		// Servizi di piattaforma Android.
#if ANDROID
		services.AddSingleton<IScreenBrightnessController, Platforms.Android.Services.ScreenBrightnessController>();
		services.AddSingleton<IReadingFilterProbe, Platforms.Android.Services.ReadingFilterProbe>();
		services.AddSingleton<IBackupScheduler, Platforms.Android.Services.AndroidBackupScheduler>();
		services.AddSingleton<IBackupNotifier, Platforms.Android.Services.AndroidBackupNotifier>();
		services.AddSingleton<IUpdateNotifier, Platforms.Android.Services.AndroidUpdateNotifier>();
		services.AddSingleton<IUpdateDownloadLauncher, Platforms.Android.Services.AndroidUpdateDownloadLauncher>();
		services.AddSingleton<IUpdateCheckScheduler, Platforms.Android.Services.AndroidUpdateCheckScheduler>();
		services.AddSingleton<IApkInstaller, Platforms.Android.Services.AndroidApkInstaller>();
#else
		services.AddSingleton<IBackupScheduler, NoopBackupScheduler>();
		services.AddSingleton<IBackupNotifier, NoopBackupNotifier>();
		services.AddSingleton<IUpdateNotifier, NoopUpdateNotifier>();
		services.AddSingleton<IUpdateDownloadLauncher, NoopUpdateDownloadLauncher>();
		services.AddSingleton<IUpdateCheckScheduler, NoopUpdateCheckScheduler>();
		services.AddSingleton<IApkInstaller, NoopApkInstaller>();
#endif
		// Storage locale SQLite (in chiaro, v1).
		services.AddSingleton<IDatabaseService, DatabaseService>();
		services.AddSingleton<ICardRepository, CardRepository>();

		// Catalogo emittenti: seed statico bundle, read-only, offline.
		services.AddSingleton<IIssuerCatalog, IssuerCatalog>();

		// Preferenze utente (chiave/valore locale su Preferences).
		services.AddSingleton<ISettingsStore, SettingsStore>();

		// Rendering barcode (ZXing.Net + SkiaSharp).
		services.AddSingleton<IBarcodeRenderer, BarcodeRenderer>();

		// Codec del payload di condivisione (QR self-contained).
		services.AddSingleton<ICardShareCodec, CardShareCodec>();

		// Backup su Google Drive: auth OAuth PKCE, client Drive REST e orchestrazione.
		// HttpClient condiviso: nessun pacchetto Microsoft.Extensions.Http referenziato.
		services.AddSingleton<HttpClient>();
		services.AddSingleton<IGoogleAuth, GoogleAuth>();
		services.AddSingleton<IDriveBackupClient, DriveBackupClient>();
		services.AddSingleton<IBackupEnvironment, MauiBackupEnvironment>();
		services.AddSingleton<IBackupService, BackupService>();

		// Navigazione / UI
		services.AddSingleton<AppShell>();
		services.AddSingleton<CardListViewModel>();
		services.AddSingleton<CardListPage>();

		// Acquisizione carta (scan / manuale): pagine e VM transient per stato fresco.
		services.AddTransient<ScanPage>();
		services.AddTransient<AddCardPage>();
		services.AddTransient<AddCardViewModel>();

		// Visualizzazione carta (barcode a schermo).
		services.AddTransient<ShowCardPage>();
		services.AddTransient<ShowCardViewModel>();

		// Condivisione carta (QR self-contained): pagina e VM transient.
		services.AddTransient<SharePage>();
		services.AddTransient<ShareCardViewModel>();

		// Modifica carta: pagina e VM transient per stato fresco.
		services.AddTransient<EditCardPage>();
		services.AddTransient<EditCardViewModel>();

		// Impostazioni: pagina e VM transient.
		services.AddTransient<SettingsPage>();
		services.AddTransient<SettingsViewModel>();

		// Backup su Google Drive: pagina e VM transient.
		services.AddTransient<BackupPage>();
		services.AddTransient<BackupViewModel>();

		// Controllo aggiornamenti: manifest statico su GitHub Pages (nessuna autenticazione, repo privato).
		services.AddSingleton<IUpdateService, UpdateService>();
		services.AddTransient<UpdatePage>();
		services.AddTransient<UpdateViewModel>();
	}
}
