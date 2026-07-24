namespace CardMaster.Services;

/// <summary>
/// Fornisce la passphrase con cui SQLCipher cifra il database locale.
/// La passphrase è custodita protetta da una chiave nell'Android Keystore.
///
/// Confine di change: in maui-shell la chiave del Keystore NON è vincolata
/// all'autenticazione utente. Il gate biometria/PIN (setUserAuthenticationRequired)
/// verrà aggiunto da maui-unlock, che si innesta su questa interfaccia.
/// </summary>
public interface IKeyStoreService
{
    /// <summary>
    /// Restituisce la passphrase del database: la genera e la custodisce al primo
    /// avvio, oppure riusa quella esistente. Non espone mai la chiave in chiaro
    /// fuori dal Keystore.
    /// </summary>
    string GetOrCreateDatabaseKey();
}
