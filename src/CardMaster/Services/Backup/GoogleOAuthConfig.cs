namespace CardMaster.Services.Backup;

/// <summary>
/// Configurazione OAuth 2.0 per il backup su Google Drive.
///
/// Con il flusso Authorization Code + PKCE il <see cref="ClientId"/> NON è un secret
/// (nessun client secret nell'APK), quindi può stare come costante di configurazione.
/// Va sostituito con il client id reale creato su Google Cloud: OAuth client di tipo
/// <b>Android</b>, legato al package name <c>com.cardmaster.app</c> e agli SHA-1 dei
/// certificati di firma di debug e release. Vedi <c>docs/google-drive-backup.md</c> (task 1.1).
/// </summary>
public static class GoogleOAuthConfig
{
    // TODO(1.1): sostituire con il client id reale del progetto Google Cloud.
    public const string ClientId = "420048733064-cri3r3ikp1rs9o6cu652gsk3alckn52e.apps.googleusercontent.com";

    // Redirect custom-scheme registrato nel manifest (intent filter del callback WebAuthenticator).
    public const string RedirectScheme = "com.cardmaster.app";
    public const string RedirectUri = "com.cardmaster.app:/oauth2redirect";

    // Ambito minimo: sola cartella applicativa nascosta (appdata) + identità account (email).
    public const string Scopes = "https://www.googleapis.com/auth/drive.appdata openid email";

    public const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    public const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";
}
