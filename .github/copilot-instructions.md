# Copilot Instructions – LawOfWriter

## Tech Stack

- **Blazor WebAssembly** (.NET 10, `net10.0`) — pure client-side, no server-side .NET
- **MudBlazor 9.x** — UI component library
- **PWA** — service worker + IndexedDB offline support
- Deployed as static files behind **nginx** (see `Dockerfile`)

## Build & Run

```bash
# From the repo root
dotnet build

# Run dev server (from LawOfWriter/ subdirectory)
cd LawOfWriter
dotnet run

# Production publish
dotnet publish LawOfWriter.csproj -c Release -o /app/publish
```

There is no test project. Debug via **browser DevTools** (F12), not IDE breakpoints — Blazor WASM runs as WebAssembly in the browser.

For WebAssembly source-level debugging: press **Shift+Option+D** (macOS) in the browser and follow the prompts.

## Architecture

The app is a single Blazor WASM project. All data comes from the REST API at `https://die.sinnnlosen.de/api`.

### Service Layer

| Service | Role |
|---|---|
| `ApiService` | Typed HTTP client — `GetAsync<T>`, `PostAsync<TReq,TResp>`, `PutAsync`, `DeleteAsync`, `GetRawAsync` |
| `ApiAuthorizationHandler` | `DelegatingHandler` — injects Bearer token on every request; auto-retries on 401 after token refresh |
| `AuthService` | JWT lifecycle — login, logout, token/refresh-token storage in `localStorage`, 6-hour expiry |
| `CustomAuthStateProvider` | Bridges `AuthService` with Blazor's `AuthenticationStateProvider` |
| `LocalDbService` | Offline storage via **IndexedDB** (JS interop to `wwwroot/js/localDb.js`) |
| `IGameDayActionService` / `GameDayActionService` | Business logic for saving actions; sets audit fields (`Created`, `Changed`, `Createdby`, `Changedby`) |

### Offline / Sync Pattern

Data is saved locally first, then synced to the API:

- `LocalDbService.SaveGameDayActionAsUnsyncedAsync(action)` — marks a record as dirty (`IsSynced = false`)
- `LocalDbService.MarkActionAsSyncedAsync(id)` — call after a successful API post
- `LocalDbService.GetUnsyncedActionsAsync()` — get all pending items
- The `LocalGameDayAction` model wraps `GameDayActionDto` and adds `IsSynced` and `LastSyncedAt`

### Authentication Flow

1. `AuthService.LoginAsync()` → POST `/auth/login` → stores JWT + refresh token + user claims in `localStorage`
2. `ApiAuthorizationHandler` reads the token on every request; on 401 it calls `AuthService.RefreshTokenAsync()` and retries once
3. `CustomAuthStateProvider.GetAuthenticationStateAsync()` reads from `AuthService` to build the Blazor `ClaimsPrincipal`
4. After logout, call `CustomAuthStateProvider.NotifyUserLogout()` to update UI state immediately

## Key Conventions

### DTOs vs Models
- `DTO/` — API transport objects (what the server sends/receives)
- `Models/` — local/UI-internal structures (e.g. `LocalGameDayAction` adds sync state on top of a DTO)

### API Calls
Always use `ApiService`, not raw `HttpClient`. Endpoints are relative and **do not** have a leading `/`:
```csharp
// Correct
await ApiService.GetAsync<List<GameApiDto>>("data/gamedays");
await ApiService.PostAsync<GameDayActionDto>("data/gameaction", item);

// Wrong — leading slash breaks the base URL
await ApiService.GetAsync<List<GameApiDto>>("/data/gamedays");
```

### Protected Pages
Add `@attribute [Authorize]` to any page that requires login. Unauthenticated users are redirected by `RedirectToLogin.razor`.

### Logging
Use `ILogger<T>` with **structured logging** (named parameters, never string interpolation). `Console.WriteLine()` does **not** work in Blazor WASM.
```csharp
// Correct
_logger.LogInformation("Saving action {Id} for user {UserId}", item.Id, userId);

// Wrong
_logger.LogInformation($"Saving action {item.Id}");
```

Logs appear in the **browser console** and are also forwarded to Seq at `https://logs.lichtii.de` (minimum level: `Information`).

### JSON Serialization
`LocalDbService` uses `JsonNamingPolicy.CamelCase` with `PropertyNameCaseInsensitive = true`. Match this when working with IndexedDB data.

### Adding a New Page
1. Create `Pages/MyPage.razor` with `@page "/my-route"`
2. Add `@attribute [Authorize]` if login is required
3. Inject services: `@inject ApiService ApiService`, etc.
4. Add a nav entry in `Layout/NavMenu.razor` if needed
