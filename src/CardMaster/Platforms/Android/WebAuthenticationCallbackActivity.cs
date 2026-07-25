using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace CardMaster.Platforms.Android;

/// <summary>
/// Riceve il callback custom-scheme del flusso OAuth (WebAuthenticator).
/// Corrisponde al redirect URI <c>com.cardmaster.app:/oauth2redirect</c> (vedi
/// <see cref="CardMaster.Services.Backup.GoogleOAuthConfig"/>).
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "com.cardmaster.app",
    DataPath = "/oauth2redirect")]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
