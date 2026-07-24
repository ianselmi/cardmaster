using System.Security.Cryptography;
using System.Text;
using Android.Security.Keystore;
using CardMaster.Services;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;

namespace CardMaster.Platforms.Android.Services;

/// <summary>
/// Implementazione Android di <see cref="IKeyStoreService"/>.
///
/// Strategia (standard per SQLCipher su Android): SQLCipher richiede una passphrase
/// come byte/stringa, ma l'Android Keystore non restituisce il materiale delle chiavi
/// che custodisce. Perciò:
///   1. Nel Keystore vive una chiave AES-GCM (il suo materiale non lascia mai il Keystore).
///   2. Si genera una passphrase casuale per il DB, la si cifra con la chiave del Keystore
///      e si salva SOLO il ciphertext (IV+dati) nelle Preferences.
///   3. All'apertura si decifra la passphrase tramite la chiave del Keystore.
///
/// Nota (decisione 24 lug 2026): la v1 NON prevede alcuna gate di sblocco applicativa
/// (niente biometria/PIN): l'app apre direttamente le carte. La chiave del Keystore
/// resta quindi senza binding all'autenticazione utente. La protezione è la cifratura
/// at-rest; la protezione "telefono in mano ad altri" è delegata al lockscreen di Android.
/// </summary>
public sealed class KeyStoreService : IKeyStoreService
{
    private const string AndroidKeyStore = "AndroidKeyStore";
    private const string KeyAlias = "cardmaster_db_key";
    private const string PrefName = "cardmaster_db_pass"; // Base64(iv|ciphertext)
    private const int GcmTagBits = 128;
    private const int PassphraseBytes = 32;

    public string GetOrCreateDatabaseKey()
    {
        var stored = Preferences.Default.Get<string?>(PrefName, null);
        if (!string.IsNullOrEmpty(stored))
        {
            return DecryptPassphrase(stored);
        }

        // Prima esecuzione: genera una passphrase casuale robusta e custodiscila cifrata.
        var passBytes = new byte[PassphraseBytes];
        RandomNumberGenerator.Fill(passBytes);
        var passphrase = Convert.ToBase64String(passBytes);

        Preferences.Default.Set(PrefName, EncryptPassphrase(passphrase));
        return passphrase;
    }

    private static ISecretKey GetOrCreateSecretKey()
    {
        var ks = KeyStore.GetInstance(AndroidKeyStore)!;
        ks.Load(null);

        if (ks.ContainsAlias(KeyAlias))
        {
            var entry = (KeyStore.SecretKeyEntry)ks.GetEntry(KeyAlias, null)!;
            return entry.SecretKey!;
        }

        var generator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, AndroidKeyStore)!;
        var spec = new KeyGenParameterSpec.Builder(
                KeyAlias,
                KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
            .SetBlockModes(KeyProperties.BlockModeGcm)!
            .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)!
            .SetKeySize(256)!
            // v1: nessun binding all'autenticazione utente (l'app apre subito le carte).
            .Build();

        generator.Init(spec);
        return generator.GenerateKey()!;
    }

    private static string EncryptPassphrase(string plaintext)
    {
        var cipher = Cipher.GetInstance("AES/GCM/NoPadding")!;
        cipher.Init(Javax.Crypto.CipherMode.EncryptMode, GetOrCreateSecretKey());

        var iv = cipher.GetIV()!;
        var ct = cipher.DoFinal(Encoding.UTF8.GetBytes(plaintext))!;

        // Formato: [ivLen(1)][iv][ciphertext+tag]
        var combined = new byte[1 + iv.Length + ct.Length];
        combined[0] = (byte)iv.Length;
        Buffer.BlockCopy(iv, 0, combined, 1, iv.Length);
        Buffer.BlockCopy(ct, 0, combined, 1 + iv.Length, ct.Length);

        return Convert.ToBase64String(combined);
    }

    private static string DecryptPassphrase(string stored)
    {
        var data = Convert.FromBase64String(stored);
        int ivLen = data[0];

        var iv = new byte[ivLen];
        Buffer.BlockCopy(data, 1, iv, 0, ivLen);

        var ct = new byte[data.Length - 1 - ivLen];
        Buffer.BlockCopy(data, 1 + ivLen, ct, 0, ct.Length);

        var cipher = Cipher.GetInstance("AES/GCM/NoPadding")!;
        cipher.Init(Javax.Crypto.CipherMode.DecryptMode, GetOrCreateSecretKey(), new GCMParameterSpec(GcmTagBits, iv));

        var pt = cipher.DoFinal(ct)!;
        return Encoding.UTF8.GetString(pt);
    }
}
