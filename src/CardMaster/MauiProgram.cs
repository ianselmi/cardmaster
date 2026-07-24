using BarcodeScanning;
using CardMaster.Services;
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
#endif
		// Storage locale SQLite (in chiaro, v1).
		services.AddSingleton<IDatabaseService, DatabaseService>();
		services.AddSingleton<ICardRepository, CardRepository>();

		// Catalogo emittenti: seed statico bundle, read-only, offline.
		services.AddSingleton<IIssuerCatalog, IssuerCatalog>();

		// Rendering barcode (ZXing.Net + SkiaSharp).
		services.AddSingleton<IBarcodeRenderer, BarcodeRenderer>();

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
	}
}
