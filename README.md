# 🎳 Law Of Writer

> Bowling-Spieltag-Tracker als Progressive Web App für Tablets & iPads

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-9.0-7B1FA2)](https://mudblazor.com/)
[![PWA](https://img.shields.io/badge/PWA-installierbar-5A0FC8?logo=pwa)](https://web.dev/progressive-web-apps/)

---

## Beschreibung

**Law Of Writer** ist eine Blazor WebAssembly PWA zur Erfassung und Verwaltung von Bowling-Spieltagen. Die App ist für die Nutzung auf Tablets und iPads optimiert und funktioniert auch offline dank IndexedDB-Speicherung mit automatischer Synchronisation.

### Hauptfunktionen

- 🎳 **Spieltag-Verwaltung** — Spieltage als Karten anzeigen, öffnen und bearbeiten
- 📊 **Zwei Ansichten** — Tabelle (DataGrid) und Schnellansicht (kompakte Karten)
- ✏️ **Aktionen erfassen** — Band, Pumpe, Kranz, Kugelfang, Alle Neune, Geld
- 🔐 **Benutzeranmeldung** — E-Mail/Passwort-Login mit JWT-Authentifizierung
- 📴 **Offline-Modus** — Vollständig offline nutzbar mit IndexedDB
- 🔄 **Auto-Sync** — Automatische Synchronisation bei Internetverbindung
- 🎲 **Losung** — Würfel-/Losungs-Seite
- 🌙 **Dark Mode** — Umschaltbarer Dark/Light-Modus
- 📱 **PWA** — Installierbar auf iPad und anderen Geräten
- 🔔 **Auto-Updates** — Service Worker erkennt neue Versionen automatisch

---

## Tech-Stack

| Komponente         | Technologie                          |
|--------------------|--------------------------------------|
| Frontend           | Blazor WebAssembly (.NET 10)         |
| UI-Framework       | MudBlazor 9                          |
| Authentifizierung  | JWT (eigener AuthStateProvider)       |
| Offline-Speicher   | IndexedDB (via JS-Interop)           |
| API                | REST (`https://die.sinnnlosen.de/api/`) |
| Deployment         | Docker + nginx                       |
| PWA                | Service Worker mit Auto-Update       |

---

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Optional: Node.js (nur falls JS-Assets angepasst werden)
- Docker (für Container-Deployment)

---

## Lokal entwickeln

```bash
# Repository klonen
git clone <repo-url>
cd LawOfWriter

# Solution bauen
dotnet build

# Dev-Server starten (im Projektordner)
cd LawOfWriter
dotnet run
```

Die App ist dann unter `https://localhost:5001` bzw. `http://localhost:5000` erreichbar.

Für Hot Reload kann alternativ `dotnet watch run` im Projektordner verwendet werden.

---

## Docker-Build

```bash
cd LawOfWriter  # In das Projektverzeichnis (nicht Solution-Root)

# Image bauen
docker build -t law-of-writer .

# Container starten
docker run -d -p 8080:80 --name law-of-writer law-of-writer
```

Die App ist dann unter `http://localhost:8080` erreichbar.

---

## Projektstruktur

```
LawOfWriter/                        # Solution-Root
├── LawOfWriter.sln
├── README.md
└── LawOfWriter/                    # Blazor WASM Projekt
    ├── LawOfWriter.csproj
    ├── Program.cs
    ├── _Imports.razor
    ├── Dockerfile
    ├── nginx.conf
    ├── Layout/
    │   ├── MainLayout.razor        # AppBar, Menü, Status-Icons
    │   ├── NavMenu.razor
    │   └── UpdateDialog.razor
    ├── Pages/
    │   ├── Login.razor             # Anmeldeseite (/)
    │   ├── Game.razor              # Spieltag-Übersicht (/game)
    │   ├── Losung.razor            # Losung/Würfel (/losung)
    │   ├── Help.razor              # Hilfe-Seite (/hilfe)
    │   └── Component/
    │       ├── GameDetails.razor
    │       ├── EditGameActionDialog.razor
    │       ├── MoneyInputDialog.razor
    │       └── NumericStepper.razor
    ├── Services/
    │   ├── ApiService.cs           # REST-API-Kommunikation
    │   ├── AuthService.cs          # Login/Logout/Token
    │   ├── ConnectivityService.cs  # Online/Offline-Erkennung & Sync
    │   ├── LocalDbService.cs       # IndexedDB-Zugriff
    │   └── ...
    ├── Models/ & DTO/
    └── wwwroot/
        ├── index.html
        ├── manifest.webmanifest
        ├── service-worker.js
        ├── service-worker.published.js
        ├── js/localDb.js
        └── css/app.css
```

---

## Architektur

```
┌─────────────────────────────────────┐
│         Blazor WASM (Browser)       │
│                                     │
│  ┌───────────┐   ┌───────────────┐  │
│  │ MudBlazor │   │  IndexedDB    │  │
│  │    UI      │   │  (Offline)    │  │
│  └─────┬─────┘   └───────┬───────┘  │
│        │                  │          │
│        └──────┬───────────┘          │
│               │                      │
│    ┌──────────▼──────────┐           │
│    │  ConnectivityService│           │
│    │  (Online/Offline    │           │
│    │   Auto-Sync)        │           │
│    └──────────┬──────────┘           │
└───────────────┼──────────────────────┘
                │ HTTPS/REST
        ┌───────▼───────┐
        │   REST API    │
        │ (JWT Auth)    │
        └───────────────┘
```

**Offline-First-Prinzip:** Alle Daten werden zuerst lokal in IndexedDB gespeichert. Sobald eine Internetverbindung besteht, synchronisiert der `ConnectivityService` automatisch alle ausstehenden Änderungen mit der API.

---

## Status-Icons in der AppBar

| Icon | Farbe | Bedeutung |
|------|-------|-----------|
| ☁️✗ | Rot | Offline – keine Verbindung |
| ☁️↑ | Gelb | Online, nicht synchronisierte Daten – tippen zum Senden |
| 🔄 | Blau | Synchronisierung läuft |
| ☁️✓ | Grün | Alle Daten synchronisiert |
| 🔔 | Gelb | App-Update verfügbar – tippen zum Installieren |
| 📱 | Blau | App kann installiert werden |

---

## Deployment

Die App wird als statische Blazor WASM-Anwendung hinter nginx ausgeliefert:

1. `dotnet publish` erzeugt statische Dateien in `wwwroot/`
2. Docker-Image kopiert diese nach nginx
3. nginx bedient die SPA mit Fallback-Routing

Die `nginx.conf` ist bereits für SPA-Routing konfiguriert (alle Routen → `index.html`).

### Produktion publizieren

```bash
dotnet publish LawOfWriter/LawOfWriter.csproj -c Release -o /app/publish
```

---

## Version

Aktuelle Version: **v0.2.1** (konfiguriert in `wwwroot/appsettings.json`)

---

## Lizenz

Privates Projekt – alle Rechte vorbehalten.
