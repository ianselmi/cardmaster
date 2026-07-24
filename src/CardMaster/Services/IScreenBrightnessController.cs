namespace CardMaster.Services;

/// <summary>
/// Controlla la luminosità dello schermo per la finestra dell'app.
/// Usato per portare al massimo la luminosità mentre una carta è aperta.
/// </summary>
public interface IScreenBrightnessController
{
    /// <summary>Porta la luminosità della finestra al massimo.</summary>
    void SetMax();

    /// <summary>Riporta la luminosità al default di sistema.</summary>
    void RestoreDefault();
}
