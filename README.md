# TaskRoute

## Descrizione

TaskRoute è un'applicazione web moderna e scalabile, progettata per semplificare la gestione delle tue commissioni quotidiane e ottimizzare i tuoi spostamenti. Grazie all'architettura basata su ASP.NET Core Razor Pages e a una UI intuitiva, TaskRoute ti permette di:

* **Centralizzare tutte le tue attività**: crea, modifica e monitora tutte le commissioni in un unico hub, assegnando dettagli come titolo, descrizione, scadenza, durata stimata e luogo di svolgimento.
* **Geolocalizzare ogni tappa**: associa a ogni commissione un indirizzo preciso (con latitudine e longitudine) per pianificare al meglio i tuoi percorsi e visualizzare i punti di interesse.
* **Ottimizzare i percorsi con Gemini**: integra il servizio esterno Gemini, il quale elabora distanze tra le tappe, orari di apertura, durata stimata di ciascuna commissione e condizioni di traffico per calcolare il percorso più efficiente. In questo modo riduci tempi di viaggio, consumi e stress, ottenendo sempre l’ordine ottimale delle tappe.
* **Interfaccia reattiva e dinamica**: utilizza AJAX per aggiornamenti in tempo reale (ad esempio, toggle di completamento attività) senza ricaricare la pagina, assicurando un'esperienza fluida sia da desktop che da dispositivi mobili.
* **Pulizia automatica**: il servizio `CompletedTaskCleanupService` elimina automaticamente le commissioni completate da più di 24 ore, mantenendo la tua lista sempre ordinata e rilevante.
* **Sicurezza e personalizzazione**: grazie ad ASP.NET Core Identity, gestisci ruoli e permessi (User, Editor, Administrator) e proteggi i dati sensibili degli utenti.

Questa combinazione di funzionalità rende TaskRoute ideale sia per utenti privati, desiderosi di organizzare meglio le commissioni di tutti i giorni, sia per piccoli team di consegna o professionisti che necessitano di massimizzare l’efficienza operativa e ridurre i costi logistici.

## Funzionalità

* **Autenticazione e Autorizzazione**

  * Basata su ASP.NET Core Identity
  * Ruoli predefiniti: User, Editor, Administrator
* **Gestione Commissioni (CRUD)**

  * Creazione, Lettura, Modifica, Eliminazione di commissioni
  * Proprietà: Titolo, Descrizione, Data di scadenza, Orario specifico opzionale, Durata stimata opzionale
* **Geolocalizzazione**

  * Associazione facoltativa di un luogo (Location) ad ogni commissione
  * Modello `Location` con Name, Address, City, PostalCode, Country, Latitude, Longitude
* **Ottimizzazione del Percorso**

  * Integrazione con il servizio Gemini tramite `GeminiService`
  * Endpoint JSON (`OnPostOptimizeAsync`) per ottimizzare l’ordine delle commissioni selezionate
* **Interfaccia Utente**

  * Pagine Razor: Index, TaskList, AddTask, EditTask, DeleteTask, Error
  * Toggle di completamento tramite AJAX (`OnPostToggleCompletedAsync`)
* **Servizio di Cleanup**

  * `CompletedTaskCleanupService` elimina automaticamente le commissioni completate da più di 24 ore
* **Seed iniziale**

  * `SeedData` per creare ruoli, utente amministratore e dati di esempio (Locations e Commissioni)

## Tecnologie Utilizzate

* **Framework e Linguaggi**:

  * ASP.NET Core 7 (.NET 7)
  * C#
  * Razor Pages
* **Persistenza**:

  * Entity Framework Core
  * SQLite (DefaultConnection)
* **Autenticazione**:

  * ASP.NET Core Identity
* **Servizi**:

  * `HttpClient` per chiamate a Gemini API
  * HostedService (`IHostedService`) per cleanup
* **Configurazione**:

  * `appsettings.json` con `DefaultConnection` e `Gemini:ApiKey`
* **Logging**:

  * `Microsoft.Extensions.Logging`

## Struttura del Progetto

```
/Pages
 ├─ Index.cshtml(.cs)
 ├─ TaskList.cshtml(.cs)
 ├─ AddTask.cshtml(.cs)
 ├─ EditTask.cshtml(.cs)
 ├─ DeleteTask.cshtml(.cs)
 └─ Error.cshtml(.cs)
/Data
 ├─ ApplicationDbContext.cs
 └─ SeedData.cs
/Models
 ├─ Commission.cs
 ├─ Location.cs
 └─ UserTask.cs
/Services
 ├─ GeminiService.cs
 └─ CompletedTaskCleanupService.cs
Program.cs
appsettings.json
```

## Configurazione e Avvio

1. Clona il repository:

   ```bash
   git clone https://github.com/tuoaccount/TaskRoute.git
   cd TaskRoute
   ```
2. Configura `appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=taskroute.db"
   },
   "Gemini": {
     "ApiKey": "<LA_TUA_API_KEY>"
   }
   ```
3. Applica migrazioni ed esegui il seed:

   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
4. Avvia l’applicazione:

   ```bash
   dotnet run
   ```

## Contributi

1. Fork del progetto
2. Crea un branch: `git checkout -b feature/nome`
3. Commit delle modifiche: `git commit -m "Descrizione della modifica"`
4. Pusha e apri una Pull Request

## Licenza

Distribuito sotto licenza MIT. Vedi `LICENSE.md` per i dettagli.
