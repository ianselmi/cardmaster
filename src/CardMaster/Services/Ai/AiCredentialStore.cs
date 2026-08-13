using Microsoft.Maui.Storage;

namespace CardMaster.Services.Ai;

/// <summary>
/// Implementazione di <see cref="IAiCredentialStore"/> su <see cref="SecureStorage"/>
/// (Keystore Android), sul modello del <c>refresh_token</c> di <c>GoogleAuth</c>.
/// </summary>
/// <remarks>
/// In <see cref="Preferences"/> vive un solo <b>indicatore booleano</b> — "una chiave esiste" —
/// perché <see cref="SecureStorage"/> è asincrono e l'interfaccia deve poter mostrare lo stato
/// senza attendere. Nelle preferenze non finisce nulla della chiave: né il valore, né un suo
/// frammento. L'indicatore può disallinearsi (il sistema può invalidare il Keystore, per esempio
/// dopo un ripristino), quindi <see cref="GetKeyAsync"/> lo corregge quando scopre che la chiave
/// non c'è più.
/// </remarks>
public sealed class AiCredentialStore : IAiCredentialStore
{
    private const string ApiKeyEntry = "anthropic_api_key";
    private const string ConfiguredFlagKey = "ai_key_configured";

    public bool IsConfigured => Preferences.Default.Get(ConfiguredFlagKey, false);

    public async Task<bool> SetKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var trimmed = apiKey?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        try
        {
            await SecureStorage.Default.SetAsync(ApiKeyEntry, trimmed).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsSecureStorageFailure(ex))
        {
            // Archivio protetto non disponibile: meglio nessuna chiave che una chiave a metà.
            Preferences.Default.Set(ConfiguredFlagKey, false);
            return false;
        }

        Preferences.Default.Set(ConfiguredFlagKey, true);
        return true;
    }

    public Task RemoveKeyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            SecureStorage.Default.Remove(ApiKeyEntry);
        }
        catch (Exception ex) when (IsSecureStorageFailure(ex))
        {
            // Se l'archivio protetto non risponde, l'indicatore va comunque spento: la funzione
            // deve risultare disattivata, che è ciò che l'utente ha chiesto.
        }

        Preferences.Default.Set(ConfiguredFlagKey, false);
        return Task.CompletedTask;
    }

    public async Task<string?> GetKeyAsync(CancellationToken cancellationToken = default)
    {
        string? key;
        try
        {
            key = await SecureStorage.Default.GetAsync(ApiKeyEntry).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsSecureStorageFailure(ex))
        {
            // Voce illeggibile (tipico dopo un ripristino su un device diverso): equivale a
            // "nessuna chiave". Si ripulisce, così l'utente vede che deve reinserirla.
            SafeRemove();
            Preferences.Default.Set(ConfiguredFlagKey, false);
            return null;
        }

        if (string.IsNullOrEmpty(key))
        {
            // L'indicatore diceva il contrario: riallinealo, altrimenti l'interfaccia mente.
            Preferences.Default.Set(ConfiguredFlagKey, false);
            return null;
        }

        return key;
    }

    private static void SafeRemove()
    {
        try
        {
            SecureStorage.Default.Remove(ApiKeyEntry);
        }
        catch (Exception ex) when (IsSecureStorageFailure(ex))
        {
            // Nulla da fare: l'indicatore spento è già sufficiente a disattivare la funzione.
        }
    }

    /// <summary>
    /// Guasti dell'archivio protetto da assorbire. Non si cattura <see cref="Exception"/> nudo
    /// perché una <see cref="OperationCanceledException"/> deve continuare a propagarsi.
    /// </summary>
    private static bool IsSecureStorageFailure(Exception ex) =>
        ex is not OperationCanceledException;
}
