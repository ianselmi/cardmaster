using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Storage;

namespace CardMaster.Services.Backup;

/// <summary>
/// Implementazione OAuth 2.0 Authorization Code + PKCE via <see cref="WebAuthenticator"/>.
/// Scambio e refresh dei token fatti a mano con <see cref="HttpClient"/> sugli endpoint di
/// Google (nessun SDK Google). Il <c>refresh_token</c> è in <see cref="SecureStorage"/>,
/// l'<c>access_token</c> in memoria con refresh silenzioso on-demand.
/// </summary>
public sealed class GoogleAuth : IGoogleAuth
{
    private const string RefreshTokenKey = "google_refresh_token";
    private const string AccountEmailKey = "google_account_email";

    private readonly HttpClient _http;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiry;

    public GoogleAuth(HttpClient http)
    {
        _http = http;
    }

    public string? AccountEmail => Preferences.Default.Get<string?>(AccountEmailKey, null);

    public bool IsSignedIn => !string.IsNullOrEmpty(AccountEmail);

    public async Task<bool> SignInAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var verifier = CreateCodeVerifier();
            var challenge = CreateCodeChallenge(verifier);

            var authUrl =
                $"{GoogleOAuthConfig.AuthEndpoint}" +
                $"?client_id={Uri.EscapeDataString(GoogleOAuthConfig.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(GoogleOAuthConfig.RedirectUri)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(GoogleOAuthConfig.Scopes)}" +
                $"&code_challenge={challenge}" +
                $"&code_challenge_method=S256" +
                $"&access_type=offline" +
                $"&prompt=consent";

            var result = await WebAuthenticator.Default.AuthenticateAsync(new WebAuthenticatorOptions
            {
                Url = new Uri(authUrl),
                CallbackUrl = new Uri(GoogleOAuthConfig.RedirectUri),
            }).ConfigureAwait(false);

            if (!result.Properties.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
            {
                return false;
            }

            var (token, _) = await ExchangeCodeAsync(code, verifier, cancellationToken).ConfigureAwait(false);
            if (token?.AccessToken is null || string.IsNullOrEmpty(token.RefreshToken))
            {
                return false;
            }

            await SecureStorage.Default.SetAsync(RefreshTokenKey, token.RefreshToken).ConfigureAwait(false);
            CacheAccessToken(token);
            Preferences.Default.Set(AccountEmailKey, ExtractEmail(token.IdToken));
            return true;
        }
        catch (TaskCanceledException)
        {
            // L'utente ha annullato il consenso: nessun errore verso il chiamante.
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            // Rete non disponibile.
            return false;
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var refresh = await SecureStorage.Default.GetAsync(RefreshTokenKey).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(refresh))
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = refresh });
                using var _ = await _http.PostAsync(GoogleOAuthConfig.RevokeEndpoint, content, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (HttpRequestException)
        {
            // Revoca best-effort: se la rete manca, cancelliamo comunque le credenziali locali.
        }
        finally
        {
            SecureStorage.Default.Remove(RefreshTokenKey);
            Preferences.Default.Remove(AccountEmailKey);
            _accessToken = null;
            _accessTokenExpiry = default;
        }
    }

    public async Task<AccessTokenResult> GetValidAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && IsAccessTokenFresh())
        {
            return Valid();
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && IsAccessTokenFresh())
            {
                return Valid();
            }

            var refresh = await SecureStorage.Default.GetAsync(RefreshTokenKey).ConfigureAwait(false);
            if (string.IsNullOrEmpty(refresh))
            {
                return new AccessTokenResult(null, TokenFailure.NoAccount);
            }

            var (token, failure) = await RefreshAsync(refresh, cancellationToken).ConfigureAwait(false);
            if (token?.AccessToken is null)
            {
                // Una risposta 2xx illeggibile è comunque un rifiuto: senza token non si procede.
                return new AccessTokenResult(null, failure == TokenFailure.None ? TokenFailure.Rejected : failure);
            }

            CacheAccessToken(token);
            if (!string.IsNullOrEmpty(token.RefreshToken))
            {
                await SecureStorage.Default.SetAsync(RefreshTokenKey, token.RefreshToken).ConfigureAwait(false);
            }

            return Valid();
        }
        finally
        {
            _gate.Release();
        }
    }

    private AccessTokenResult Valid() => new(_accessToken, TokenFailure.None);

    private bool IsAccessTokenFresh() =>
        _accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiry - TimeSpan.FromMinutes(1);

    private void CacheAccessToken(TokenResponse token)
    {
        _accessToken = token.AccessToken;
        _accessTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
    }

    private async Task<(TokenResponse? Token, TokenFailure Failure)> ExchangeCodeAsync(string code, string verifier, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = GoogleOAuthConfig.ClientId,
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = GoogleOAuthConfig.RedirectUri,
        });

        return await PostTokenAsync(content, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(TokenResponse? Token, TokenFailure Failure)> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = GoogleOAuthConfig.ClientId,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        });

        return await PostTokenAsync(content, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Chiama il token endpoint distinguendo il rifiuto del server (4xx, tipicamente
    /// <c>invalid_grant</c> su refresh token revocato: definitivo, serve riconnettersi) da un
    /// errore di trasporto o 5xx (transitorio: è solo la rete, non le credenziali).
    /// </summary>
    private async Task<(TokenResponse? Token, TokenFailure Failure)> PostTokenAsync(HttpContent content, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.PostAsync(GoogleOAuthConfig.TokenEndpoint, content, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return (null, (int)response.StatusCode is >= 400 and < 500
                    ? TokenFailure.Rejected
                    : TokenFailure.Network);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var token = JsonSerializer.Deserialize(json, BackupJsonContext.Default.TokenResponse);
            return token is null ? (null, TokenFailure.Rejected) : (token, TokenFailure.None);
        }
        catch (HttpRequestException)
        {
            return (null, TokenFailure.Network);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, TokenFailure.Network);
        }
    }

    private static string CreateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64Url(bytes);
    }

    private static string CreateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64Url(hash);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Estrae il claim <c>email</c> dal payload dell'id_token (JWT). Best-effort.</summary>
    private static string? ExtractEmail(string? idToken)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            return null;
        }

        var parts = idToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            return doc.RootElement.TryGetProperty("email", out var email) ? email.GetString() : null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
