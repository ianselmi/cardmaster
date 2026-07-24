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
		// Storage cifrato: chiave nel Keystore -> passphrase SQLCipher -> DB.
#if ANDROID
		services.AddSingleton<IKeyStoreService, Platforms.Android.Services.KeyStoreService>();
#endif
		services.AddSingleton<IDatabaseService, DatabaseService>();
		services.AddSingleton<ICardRepository, CardRepository>();

		// Navigazione / UI
		services.AddSingleton<AppShell>();
		services.AddSingleton<CardListViewModel>();
		services.AddSingleton<CardListPage>();
	}
}
