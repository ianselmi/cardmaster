## Context

`SettingsPage`/`SettingsViewModel` sono già `Transient` (nuova istanza a ogni navigazione, vedi `MauiProgram.cs`): tornando da `BackupPage` a Impostazioni, `ISettingsStore.BackupEnabled` viene già riletto da zero senza bisogno di logica di refresh aggiuntiva. La change è puramente di presentazione: nessun nuovo stato persistito, nessuna nuova dipendenza.

## Goals / Non-Goals

**Goals:**
- Rendere visibile, direttamente sul pulsante "Backup su Google Drive" in Impostazioni, se il backup è attivo o no.

**Non-Goals:**
- Non cambia azioni/logica di `cloud-backup` (abilitazione, backup, ripristino, frequenza) né la UI di `BackupPage`.
- Non aggiunge polling o osservazione in tempo reale dello stato: basta la lettura al momento in cui la pagina viene mostrata (già garantita dal ciclo di vita Transient).

## Decisions

- **Sorgente dello stato**: `ISettingsStore.BackupEnabled` (già esistente), letto in `SettingsViewModel` esattamente come già avviene per `Theme`/`AppVersion`. Nessuna nuova interfaccia.
- **Presentazione**: sottotitolo di stato sotto il pulsante ("Backup attivo" / "Backup non attivo"), stesso pattern testuale già usato altrove nella pagina (es. `AppVersion` sotto `AppName`), più uno scambio di stile del pulsante (colore pieno vs. outline) quando attivo — riuso degli stili esistenti in `Styles.xaml`, nessun nuovo colore da introdurre nella palette. Alternativa scartata: badge/icona separata → introdurrebbe un asset grafico nuovo per un'informazione già esprimibile a testo, sproporzionato per una change così piccola.
- **Refresh**: nessuna sottoscrizione a eventi; la rilettura avviene per costruzione a ogni apertura della pagina (VM transient). Alternativa scartata: osservare `BackupViewModel`/eventi di stato in tempo reale → non necessario dato che l'utente non può cambiare lo stato da un'altra pagina mentre Impostazioni è visibile (navigazione singola, non split-view).

## Risks / Trade-offs

- [Rischio] Se in futuro Impostazioni smettesse di essere ricreata a ogni navigazione (VM diventasse singleton), lo stato mostrato potrebbe restare stantio. → Mitigazione: non nel perimetro di questa change; da rivalutare se/quando cambia il lifetime del ViewModel.
