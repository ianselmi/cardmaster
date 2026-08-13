## MODIFIED Requirements

### Requirement: Nessuna funzione di rete e nessun segreto nell'applicazione

L'acquisizione, il riconoscimento, il salvataggio e la consultazione degli scontrini MUST funzionare interamente offline. Il sistema MUST NOT contenere alcuna credenziale, chiave o token incorporato nel codice sorgente o nel pacchetto dell'applicazione: nessun segreto ricavabile scaricando il repository pubblico o estraendo l'APK installato.

Esiste **una sola eccezione al trasferimento dei dati**, e non è attiva per default: la **rilettura di uno scontrino tramite modello**, che invia l'immagine a un servizio esterno. Vale solo quando l'utente ha attivato esplicitamente la funzione, ha fornito una **propria** chiave, e la chiede per quello scontrino. Con la funzione spenta — che è lo stato iniziale — nessun dato dello scontrino lascia il device, e il percorso offline resta completo: acquisire, riconoscere, correggere, salvare e consultare non richiedono in nessun caso la rete.

#### Scenario: Funzionamento completo in modalità aereo

- **WHEN** il device è in modalità aereo
- **THEN** acquisizione, riconoscimento, salvataggio, consultazione e viste di spesa funzionano senza alcuna differenza

#### Scenario: Nessun dato dello scontrino lascia il device per default

- **WHEN** uno scontrino viene acquisito e salvato senza che l'utente abbia attivato la rilettura tramite modello
- **THEN** né l'immagine, né il testo riconosciuto, né i dati estratti vengono trasmessi ad alcun servizio esterno

#### Scenario: Trasferimento solo su richiesta esplicita

- **WHEN** l'utente ha attivato la rilettura tramite modello e la chiede per uno scontrino
- **THEN** viene inviata l'immagine di quel solo scontrino, dopo che l'app ha dichiarato che cosa esce e a carico di chi

#### Scenario: Nessun segreto estraibile dall'applicazione

- **WHEN** si ispeziona il repository pubblico o il pacchetto dell'applicazione distribuito
- **THEN** non è presente alcuna chiave o credenziale utilizzabile da terzi, nemmeno per la funzione di rilettura, la cui chiave è fornita dall'utente
