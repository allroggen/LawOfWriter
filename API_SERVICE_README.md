# API Service - Dokumentation

## Übersicht

Der `ApiService` ist ein zentraler HTTP-Client-Service für die Kommunikation mit der API unter `https://die.sinnnlosen.de/api`. Er fügt automatisch den Bearer Token aus dem Browser LocalStorage zu allen Anfragen hinzu.

## Architektur

Das System besteht aus drei Hauptkomponenten:

1. **ApiAuthorizationHandler**: Ein DelegatingHandler, der automatisch den Bearer Token zu allen HTTP-Requests hinzufügt
2. **ApiService**: Der zentrale Service für API-Anfragen mit typisierten Methoden
3. **AuthService**: Verwaltet die Authentifizierung und speichert den Token im LocalStorage

## Verwendung

### Dependency Injection

Der `ApiService` wird automatisch über Dependency Injection bereitgestellt:

```csharp
@inject ApiService ApiService
```

### GET Requests

```csharp
// Einfacher GET Request
var testData = await ApiService.GetAsync<TestDataResponse>("/testdata");

// GET einer Liste
var games = await ApiService.GetAsync<List<RootGame>>("/games");

// GET mit ID im Pfad
var game = await ApiService.GetAsync<RootGame>("/games/123");
```

### POST Requests

```csharp
// POST mit Request und Response
var createRequest = new CreateGameRequest { Name = "Neues Spiel" };
var newGame = await ApiService.PostAsync<CreateGameRequest, RootGame>("/games", createRequest);

// POST ohne Response (nur Success-Status)
var actionData = new GameAction { Type = "move" };
var success = await ApiService.PostAsync("/games/action", actionData);
```

### PUT Requests

```csharp
// PUT mit Request und Response
var updateData = new UpdateGameDayRequest { Day = 5 };
var updated = await ApiService.PutAsync<UpdateGameDayRequest, GameDay>("/games/123/day", updateData);
```

### DELETE Requests

```csharp
// DELETE Request
var deleted = await ApiService.DeleteAsync("/games/123");
```

### Raw HTTP Responses

Für spezielle Fälle, bei denen Sie die rohe `HttpResponseMessage` benötigen:

```csharp
var response = await ApiService.GetRawAsync("/special-endpoint");
if (response.IsSuccessStatusCode)
{
    var content = await response.Content.ReadAsStringAsync();
    // Verarbeiten Sie den Content manuell
}
```

## Fehlerbehandlung

Alle Methoden werfen Exceptions bei Fehlern. Verwenden Sie `try-catch` für die Fehlerbehandlung:

```csharp
try
{
    var data = await ApiService.GetAsync<MyData>("/endpoint");
}
catch (HttpRequestException ex)
{
    // Netzwerkfehler oder HTTP-Fehler
    Console.WriteLine($"Fehler: {ex.Message}");
}
catch (Exception ex)
{
    // Andere Fehler (z.B. Deserialisierung)
    Console.WriteLine($"Fehler: {ex.Message}");
}
```

## Beispiel: Vollständige Komponente

```csharp
@page "/games"
@using LawOfWriter.Services
@using LawOfWriter.Models
@inject ApiService ApiService

@attribute [Authorize]

<h3>Spiele</h3>

@if (loading)
{
    <p>Lade Daten...</p>
}
else if (games != null)
{
    <ul>
        @foreach (var game in games)
        {
            <li>@game.Name</li>
        }
    </ul>
}

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger">@errorMessage</div>
}

<button @onclick="CreateNewGame">Neues Spiel erstellen</button>

@code {
    private List<RootGame>? games;
    private string? errorMessage;
    private bool loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadGames();
    }

    private async Task LoadGames()
    {
        try
        {
            loading = true;
            errorMessage = null;
            games = await ApiService.GetAsync<List<RootGame>>("/games");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            loading = false;
        }
    }

    private async Task CreateNewGame()
    {
        try
        {
            var request = new CreateGameRequest { Name = "Mein Spiel" };
            var newGame = await ApiService.PostAsync<CreateGameRequest, RootGame>("/games", request);
            
            if (newGame != null)
            {
                await LoadGames(); // Liste neu laden
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
    }
}
```

## Konfiguration

Die API Base URL ist in `ApiService.cs` definiert:

```csharp
private const string ApiBaseUrl = "https://die.sinnnlosen.de/api";
```

Alle API-Endpunkte werden relativ zu dieser URL aufgerufen. Der Endpoint-Parameter sollte mit einem `/` beginnen (z.B. `/games`).

## Authentifizierung

Der Bearer Token wird automatisch hinzugefügt, wenn:
1. Der Benutzer sich über die Login-Seite angemeldet hat
2. Der Token im LocalStorage gespeichert ist (Key: "authToken")
3. Der Token nicht abgelaufen ist (12 Stunden Gültigkeit)

Wenn kein gültiger Token vorhanden ist, werden Requests ohne Authorization Header gesendet.

## Vorteile dieser Implementierung

- ✅ Automatische Token-Injection bei allen API-Calls
- ✅ Typsichere Requests und Responses
- ✅ Zentrale Fehlerbehandlung
- ✅ Einfache Verwendung - nur Endpoint angeben
- ✅ DRY Prinzip - keine Code-Wiederholung
- ✅ Leicht testbar und wartbar
