# 🐛 Debugging Guide für Blazor WebAssembly

## Problem: Breakpoints funktionieren nicht

**Grund:** Blazor WebAssembly läuft im Browser als WebAssembly-Code, nicht auf dem Server. Rider/Visual Studio können nicht direkt in den Browser debuggen.

## ✅ Lösung 1: Browser Console verwenden (EMPFOHLEN)

### Schritte:
1. **Starten Sie die Anwendung** in Rider (F5 oder Run)
2. **Öffnen Sie die Browser DevTools:**
   - **Chrome/Edge:** `F12` oder `Cmd + Option + I` (macOS) / `Ctrl + Shift + I` (Windows)
   - **Firefox:** `F12` oder `Cmd + Option + K` (macOS) / `Ctrl + Shift + K` (Windows)
   - **Safari:** `Cmd + Option + C` (macOS, Developer Menu muss aktiviert sein)

3. **Gehen Sie zum Console Tab**
4. Sie sollten sehen:
   ```
   🚀 LawOfWriter App Loading...
   Browser console is active and ready for debugging
   ```

5. **Interagieren Sie mit der App** - alle `await JS.InvokeVoidAsync("console.log", ...)` Ausgaben erscheinen hier

### Beispiel Ausgaben:
```
LoadTestData started
Making API call to /data/test
API call successful: true
API Response: {...}
```

## ✅ Lösung 2: Browser WebAssembly Debugging

### Schritte:
1. **Starten Sie die Anwendung**
2. Im Browser drücken Sie:
   - **Windows/Linux:** `Shift + Alt + D`
   - **macOS:** `Shift + Option + D`
3. Folgen Sie den Browser-Anweisungen
4. Ein neues Debug-Fenster öffnet sich
5. Öffnen Sie DevTools (F12) im neuen Fenster
6. Gehen Sie zum **Sources** Tab
7. Drücken Sie `Cmd/Ctrl + P` und suchen Sie nach Ihrer Datei (z.B. "Start.razor")
8. Setzen Sie Breakpoints im C# Code

## 📝 Logging im Code

Verwenden Sie `IJSRuntime` für Console-Ausgaben:

```csharp
@inject IJSRuntime JS

// In Ihrem Code:
await JS.InvokeVoidAsync("console.log", "Debug message");
await JS.InvokeVoidAsync("console.log", "Value:", someVariable);
await JS.InvokeVoidAsync("console.error", "Error message");
await JS.InvokeVoidAsync("console.warn", "Warning message");
```

**WICHTIG:** `Console.WriteLine()` funktioniert NICHT in Blazor WebAssembly!

## 🔍 Network Tab für API Calls

Um API-Aufrufe zu debuggen:
1. Öffnen Sie DevTools (F12)
2. Gehen Sie zum **Network** Tab
3. Filtern Sie nach "Fetch/XHR"
4. Klicken Sie auf einen Request um Details zu sehen:
   - Headers (inkl. Bearer Token)
   - Request Payload
   - Response
   - Status Code

## 🎯 Praktische Tipps

### 1. Console immer offen haben
Gewöhnen Sie sich an, die Browser Console immer geöffnet zu haben während der Entwicklung.

### 2. Strukturiertes Logging
```csharp
await JS.InvokeVoidAsync("console.group", "LoadTestData");
await JS.InvokeVoidAsync("console.log", "Starting API call");
await JS.InvokeVoidAsync("console.log", "URL:", url);
// ... Code ...
await JS.InvokeVoidAsync("console.groupEnd");
```

### 3. Conditional Logging
```csharp
#if DEBUG
await JS.InvokeVoidAsync("console.log", "Debug info");
#endif
```

### 4. Error Handling mit Details
```csharp
catch (Exception ex)
{
    await JS.InvokeVoidAsync("console.error", "Error:", ex.Message);
    await JS.InvokeVoidAsync("console.error", "Stack:", ex.StackTrace);
    await JS.InvokeVoidAsync("console.error", "Full Exception:", ex.ToString());
}
```

## 🚀 Performance Profiling

1. Öffnen Sie DevTools → **Performance** Tab
2. Klicken Sie auf "Record"
3. Interagieren Sie mit der App
4. Stoppen Sie die Aufnahme
5. Analysieren Sie die Flamegraph

## 📱 Mobile Debugging

### Chrome Remote Debugging (Android):
1. Verbinden Sie Ihr Android-Gerät
2. Öffnen Sie `chrome://inspect` in Chrome
3. Wählen Sie Ihr Gerät aus

### Safari Web Inspector (iOS):
1. Aktivieren Sie "Web Inspector" auf dem iOS Gerät
2. Öffnen Sie Safari → Develop → [Ihr Gerät]

## ⚙️ Konfiguration

Die folgenden Dateien wurden für besseres Debugging konfiguriert:

- `LawOfWriter.csproj`: Debug-Symbole aktiviert
- `launchSettings.json`: inspectUri für Browser-Debugging
- `Program.cs`: Logging Level auf Debug gesetzt
- `index.html`: Console-Test-Script hinzugefügt

## 🆘 Troubleshooting

### "console.log wird nicht angezeigt"
- ✅ Browser Console geöffnet? (F12)
- ✅ Console Tab ausgewählt?
- ✅ Filter auf "All levels" gesetzt?
- ✅ Projekt neu gebaut? (`dotnet build`)

### "Browser öffnet nicht automatisch"
- Öffnen Sie manuell: http://localhost:62720
- Prüfen Sie `launchSettings.json` → `launchBrowser: true`

### "HTTPS Fehler"
- Projekt ist auf HTTP konfiguriert (Port 62720)
- Falls HTTPS benötigt: `dotnet dev-certs https --trust`

## 📚 Weitere Ressourcen

- [Blazor WebAssembly Debugging Docs](https://docs.microsoft.com/en-us/aspnet/core/blazor/debug)
- [Browser DevTools Guide](https://developer.chrome.com/docs/devtools/)
- [JavaScript Interop in Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/)
