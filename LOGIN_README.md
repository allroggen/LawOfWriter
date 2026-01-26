# Login-Funktionalität - Anleitung

## Übersicht
Diese Implementierung bietet eine vollständige JWT-basierte Authentifizierung für Ihre Blazor WebAssembly-Anwendung.

## Erstellte Dateien

### Models
- `LoginRequest.cs` - Modell für Login-Anfragen
- `LoginResponse.cs` - Modell für Login-Antworten mit Token
- `TestDataResponse.cs` - Modell für Test-Daten

### Services
- `AuthService.cs` - Hauptservice für Authentifizierung, Token-Verwaltung und API-Aufrufe
- `CustomAuthStateProvider.cs` - Verwaltung des Authentifizierungsstatus

### Pages
- `Login.razor` - Login-Seite mit Email und Passwort-Eingabe
- `TestData.razor` - Geschützte Seite zum Testen der API mit Bearer-Token

## Funktionen

### 1. Login
- Navigieren Sie zu `/login`
- Geben Sie Email und Passwort ein
- Bei erfolgreichem Login wird der JWT-Token im Browser LocalStorage gespeichert
- Automatische Weiterleitung zur Startseite

### 2. Token-Verwaltung
- Der Token wird automatisch im LocalStorage gespeichert
- Bei jedem API-Aufruf wird der Token als Bearer-Token im Authorization-Header mitgesendet
- Der Token bleibt auch nach Browser-Refresh erhalten

### 3. Test-Daten abrufen
- Navigieren Sie zu `/testdata`
- Klicken Sie auf "Test-Daten laden"
- Die API wird mit dem Bearer-Token aufgerufen
- Die Antwort wird angezeigt

### 4. Logout
- Auf der TestData-Seite gibt es einen "Abmelden"-Button
- Der Token wird aus dem LocalStorage entfernt
- Weiterleitung zur Login-Seite

## API-Endpunkte

### Login
- **URL**: `https://die.sinnnlosen.de/api/login`
- **Methode**: POST
- **Body**: 
  ```json
  {
    "username": "alexander@allroggen.nl",
    "password": "doTho3-s"
  }
  ```
- **Antwort**:
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
  ```

### Test-Daten
- **URL**: `https://die.sinnnlosen.de/api/data/test`
- **Methode**: GET
- **Header**: `Authorization: Bearer {token}`
- **Antwort**:
  ```json
  {
    "user": "alexander@allroggen.nl",
    "message": "JWT Auth funktioniert ✅"
  }
  ```

## Verwendung

### Anmelden
1. Starten Sie die Anwendung
2. Navigieren Sie zu `/login`
3. Verwenden Sie die Test-Credentials:
   - Email: `alexander@allroggen.nl`
   - Passwort: `doTho3-s`
4. Klicken Sie auf "Anmelden"

### Geschützte Seiten nutzen
Nach erfolgreichem Login können Sie auf geschützte Seiten zugreifen. Verwenden Sie `<AuthorizeView>` in Ihren Komponenten:

```razor
<AuthorizeView>
    <Authorized>
        <!-- Inhalt für angemeldete Benutzer -->
    </Authorized>
    <NotAuthorized>
        <!-- Inhalt für nicht angemeldete Benutzer -->
    </NotAuthorized>
</AuthorizeView>
```

## Anpassungen

### Eigene API-Endpunkte
Um eigene API-Endpunkte hinzuzufügen, erweitern Sie die `AuthService.cs`:

```csharp
public async Task<YourModel?> GetYourDataAsync()
{
    var token = await GetTokenAsync();
    if (string.IsNullOrEmpty(token))
        return null;

    SetAuthorizationHeader(token);
    
    var response = await _httpClient.GetAsync($"{ApiBaseUrl}/your/endpoint");
    
    if (response.IsSuccessStatusCode)
    {
        return await response.Content.ReadFromJsonAsync<YourModel>();
    }

    return null;
}
```

### Automatische Weiterleitung zur Login-Seite
Fügen Sie in geschützten Seiten folgendes hinzu:

```csharp
protected override async Task OnInitializedAsync()
{
    var isAuthenticated = await AuthService.IsAuthenticatedAsync();
    if (!isAuthenticated)
    {
        NavigationManager.NavigateTo("/login");
    }
}
```

## Sicherheitshinweise
- Der Token wird im LocalStorage gespeichert - für Produktionsumgebungen sollten Sie zusätzliche Sicherheitsmaßnahmen in Betracht ziehen
- Der Token hat ein Ablaufdatum (exp-Claim) - Sie sollten Token-Refresh implementieren
- Verwenden Sie HTTPS in Produktionsumgebungen

## Nächste Schritte
- Token-Refresh-Funktionalität implementieren
- Automatische Abmeldung bei abgelaufenem Token
- Benutzer-Profil aus JWT-Token extrahieren und anzeigen
- Fehlerbehandlung für Netzwerkfehler verbessern
