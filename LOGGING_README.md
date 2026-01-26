# API Service Logging - Dokumentation

## Übersicht

Das API Service Logging System bietet umfassendes Logging für alle HTTP-Requests zu Ihrer API. Es nutzt die Standard .NET ILogger-Infrastruktur und loggt automatisch alle API-Anfragen, Responses und Fehler.

## Logging-Levels

### Information (LogInformation)
- **Initialisierung**: Wenn der ApiService erstellt wird
- **Request Start**: Bei jedem API-Aufruf (GET, POST, PUT, DELETE)
- **Request Success**: Bei erfolgreichen Anfragen

### Debug (LogDebug)
- **HTTP Status Codes**: Der Status Code jeder Response
- **Token Injection**: Wenn der Bearer Token zu einem Request hinzugefügt wird

### Warning (LogWarning)
- **Fehlgeschlagene Requests**: Wenn ein Request einen Fehler-Statuscode zurückgibt
- **Fehlender Token**: Wenn kein gültiger Token für einen Request verfügbar ist

### Error (LogError)
- **HTTP Errors**: Alle HTTP-Fehler mit StatusCode
- **Unexpected Errors**: Alle unerwarteten Exceptions

## Logging-Beispiele

### Erfolgreicher GET Request
```
[Information] ApiService initialized with base URL: https://die.sinnnlosen.de/api
[Information] GET Request: /games (Type: List`1)
[Debug] Bearer token added to request: GET https://die.sinnnlosen.de/api/games
[Information] GET Request successful: /games
```

### POST Request mit Fehler
```
[Information] POST Request: /games (RequestType: CreateGameRequest, ResponseType: RootGame)
[Debug] Bearer token added to request: POST https://die.sinnnlosen.de/api/games
[Debug] POST Response Status: BadRequest for /games
[Error] HTTP Error during POST request to /games. Status: BadRequest
```

### Request ohne Token
```
[Information] GET Request: /public/data (Type: PublicData)
[Warning] No valid token available for request: GET https://die.sinnnlosen.de/api/public/data
[Information] GET Request successful: /public/data
```

### DELETE Request erfolgreich
```
[Information] DELETE Request: /games/123
[Debug] Bearer token added to request: DELETE https://die.sinnnlosen.de/api/games/123
[Debug] DELETE Response Status: OK for /games/123
[Information] DELETE Request successful: /games/123
```

## Logging in der Browser Console anzeigen

In Blazor WebAssembly werden die Logs standardmäßig in der Browser-Konsole angezeigt.

### Browser Console öffnen
- **Chrome/Edge**: F12 oder Rechtsklick → "Untersuchen" → Console Tab
- **Firefox**: F12 oder Rechtsklick → "Element untersuchen" → Konsole Tab
- **Safari**: Cmd+Option+C → Konsole Tab

### Log-Level filtern
In `Program.cs` können Sie das Log-Level konfigurieren:

```csharp
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Alle Logs anzeigen
builder.Logging.SetMinimumLevel(LogLevel.Information); // Nur Info und höher
builder.Logging.SetMinimumLevel(LogLevel.Warning); // Nur Warnings und Errors
```

### Spezifisches Logging für ApiService
```csharp
builder.Logging.AddFilter("LawOfWriter.Services.ApiService", LogLevel.Debug);
builder.Logging.AddFilter("LawOfWriter.Services.ApiAuthorizationHandler", LogLevel.Debug);
```

## Geloggte Informationen

### ApiService Logs
| Methode | Was wird geloggt |
|---------|-----------------|
| `GetAsync<T>` | Endpoint, Response Type, Status Code, Errors |
| `PostAsync<TRequest, TResponse>` | Endpoint, Request Type, Response Type, Status Code, Errors |
| `PostAsync<TRequest>` | Endpoint, Request Type, Success/Failure, Status Code |
| `PutAsync<TRequest, TResponse>` | Endpoint, Request Type, Response Type, Status Code, Errors |
| `DeleteAsync` | Endpoint, Success/Failure, Status Code |
| `GetRawAsync` | Endpoint, Status Code, Errors |

### ApiAuthorizationHandler Logs
| Event | Was wird geloggt |
|-------|-----------------|
| Token hinzugefügt | HTTP Method, Request URI |
| Kein Token verfügbar | HTTP Method, Request URI (Warning) |

## Strukturierte Logs

Alle Logs verwenden strukturierte Logging-Parameter:

```csharp
_logger.LogInformation("GET Request: {Endpoint} (Type: {ResponseType})", 
    endpoint, typeof(T).Name);
```

Dies ermöglicht:
- ✅ Einfaches Filtern und Suchen
- ✅ Bessere Log-Aggregation in Production
- ✅ Konsistente Log-Formate

## Production Logging

Für Production-Umgebungen können Sie externe Logging-Provider hinzufügen:

### Application Insights (Azure)
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Serilog
```csharp
builder.Logging.AddSerilog();
```

### Seq
```csharp
builder.Logging.AddSeq("http://localhost:5341");
```

## Best Practices

1. **Debug-Logs in Development**: Nutzen Sie `LogLevel.Debug` während der Entwicklung
2. **Information in Production**: Setzen Sie auf `LogLevel.Information` oder höher in Production
3. **Sensitive Daten vermeiden**: Loggen Sie keine Passwörter, Tokens oder persönliche Daten
4. **Structured Logging nutzen**: Verwenden Sie immer Parameter statt String-Interpolation

## Troubleshooting

### Logs werden nicht angezeigt
1. Prüfen Sie das Log-Level in `Program.cs`
2. Öffnen Sie die Browser Developer Console
3. Stellen Sie sicher, dass keine Console-Filter aktiv sind

### Zu viele Logs
Reduzieren Sie das Log-Level:
```csharp
builder.Logging.SetMinimumLevel(LogLevel.Warning);
```

### Performance-Probleme
Debug-Logs können die Performance beeinträchtigen. In Production:
```csharp
#if DEBUG
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
    builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif
```

## Beispiel-Code für eigene Komponenten

Sie können auch in Ihren eigenen Komponenten auf die Logs zugreifen:

```csharp
@page "/mypage"
@inject ApiService ApiService
@inject ILogger<MyPage> Logger

@code {
    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Loading games...");
        
        try
        {
            var games = await ApiService.GetAsync<List<RootGame>>("/games");
            Logger.LogInformation("Loaded {Count} games", games?.Count ?? 0);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load games");
        }
    }
}
```

## Zusammenfassung

Das Logging-System bietet:
- ✅ Automatisches Logging aller API-Requests
- ✅ Detaillierte Fehlerinformationen
- ✅ Token-Tracking (ohne den Token selbst zu loggen)
- ✅ Status Code Logging
- ✅ Performance-Monitoring möglich
- ✅ Production-ready mit externen Providern erweiterbar
