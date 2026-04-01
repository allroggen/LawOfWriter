# AGENTS.md — LawOfWriter

## Project Overview

Blazor WebAssembly PWA (.NET 10, `net10.0`) — a bowling game-day tracker with offline-first IndexedDB storage and REST API sync. Single project, no server-side .NET. Deployed as static files behind nginx in Docker.

**Tech stack:** Blazor WASM, MudBlazor 9.x, System.Text.Json, IndexedDB (JS interop), JWT auth, Seq logging.

## Build / Run / Test

```bash
# Build (from repo root)
dotnet build

# Run dev server (from project subdirectory)
dotnet run --project LawOfWriter/LawOfWriter.csproj

# Production publish
dotnet publish LawOfWriter/LawOfWriter.csproj -c Release -o /app/publish
```

There is **no test project**. Debug via browser DevTools (F12). Blazor WASM runs as WebAssembly in the browser — IDE breakpoints do not work. For source-level debugging: press **Shift+Option+D** (macOS) in the browser.

## Project Structure

```
LawOfWriter.sln                  # Solution (single project)
LawOfWriter/
  LawOfWriter.csproj             # Blazor WASM, net10.0, PWA
  Program.cs                     # Entry point, DI registration (top-level statements)
  DTO/                           # API transport objects (server send/receive)
  Models/                        # Local/UI models (e.g. LocalGameDayAction adds sync state)
  Services/                      # Business logic and infrastructure
  Pages/                         # Routable Razor pages
  Pages/Component/               # Reusable sub-components
  Layout/                        # MainLayout, NavMenu, UpdateDialog
  wwwroot/js/localDb.js          # IndexedDB interop
  wwwroot/js/drawingCanvas.js    # Handwriting canvas
  wwwroot/appsettings.json       # Runtime config (Seq logging)
```

## Architecture

### Key Services

| Service | Role |
|---|---|
| `ApiService` | Typed HTTP client (`GetAsync<T>`, `PostAsync<TReq,TResp>`, etc.) |
| `ApiAuthorizationHandler` | DelegatingHandler — injects Bearer token, auto-retries on 401 |
| `AuthService` | JWT lifecycle — login, logout, token storage in localStorage |
| `LocalDbService` | Offline storage via IndexedDB (JS interop) |
| `IGameDayActionService` | Business logic for saving actions with audit fields |
| `ConnectivityService` | Online/offline detection, background sync |

### Offline / Sync Pattern

Data is saved locally first (`LocalDbService.SaveGameDayActionAsUnsyncedAsync`), then synced to the API. `LocalGameDayAction` wraps `GameDayActionDto` adding `IsSynced` and `LastSyncedAt`.

### Authentication Flow

1. `AuthService.LoginAsync()` -> POST `/auth/login` -> stores JWT + refresh token in localStorage
2. `ApiAuthorizationHandler` injects token on every request; on 401 calls `RefreshTokenAsync()` and retries once
3. `CustomAuthStateProvider` reads from `AuthService` to build the Blazor `ClaimsPrincipal`

## Code Style

### Namespaces and Imports

- **File-scoped namespaces** everywhere: `namespace LawOfWriter.Services;`
- **Implicit usings enabled** — only add explicit `using` for non-implicit namespaces
- Razor global imports live in `_Imports.razor`
- `Program.cs` uses top-level statements (no `Main` method)

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes, methods, properties | PascalCase | `ApiService`, `GetTokenAsync()` |
| Private fields | `_camelCase` | `_httpClient`, `_logger` |
| Local variables, parameters | camelCase | `var token`, `string endpoint` |
| Constants | PascalCase | `TokenKey`, `ApiBaseUrl` |
| Async methods | `Async` suffix | `SaveGameDayActionAsync()` |
| Razor parameters | PascalCase with `[Parameter]` | `public required GameApiDto GameDay` |
| Event callbacks | `OnXxx` or `XxxChanged` | `OnActionChanged`, `ValueChanged` |

### Formatting

- **4-space indentation** (no tabs)
- **Allman braces** in `.cs` files (opening brace on its own line)
- **K&R braces** in Razor `@code` blocks (opening brace on same line)
- Line length ~120 characters max

### Types and Language Features

- **Nullable reference types enabled** — annotate nullability (`string?`, `T?`)
- Prefer `var` for local variables
- Use C# 12 collection expressions (`[]`) for empty collections
- Use `required` on mandatory parameters/properties
- Use target-typed `new()` where type is clear from context
- No records — all types are plain classes
- Access modifiers always explicit (`public`, `private`, `private static`)

### DTOs vs Models

- `DTO/` — API transport objects matching server contract. PascalCase properties with `JsonNamingPolicy.CamelCase` serializer options.
- `Models/` — local/UI structures. Some legacy models use camelCase properties with `[JsonPropertyName]` attributes — follow the existing pattern in each file.

### API Calls

Always use `ApiService`, never raw `HttpClient`. Endpoints are relative **without** a leading `/`:

```csharp
// Correct
await ApiService.GetAsync<List<GameApiDto>>("data/gamedays");

// Wrong — leading slash breaks the base URL
await ApiService.GetAsync<List<GameApiDto>>("/data/gamedays");
```

### JSON Serialization

`System.Text.Json` exclusively (no Newtonsoft). `LocalDbService` defines shared options:

```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
```

### Error Handling

Two primary patterns:

1. **Catch-log-rethrow** (when callers should handle failure):
   ```csharp
   catch (HttpRequestException ex)
   {
       _logger.LogError(ex, "HTTP error on GET {Endpoint}: {Status}", endpoint, ex.StatusCode);
       throw;
   }
   ```

2. **Catch-log-return-default** (when failure is recoverable):
   ```csharp
   catch (Exception ex)
   {
       _logger.LogError(ex, "Login failed");
       return false;
   }
   ```

### Logging

Use `ILogger<T>` with **structured logging** (message templates, never interpolation). `Console.WriteLine()` does not work in Blazor WASM.

```csharp
// Correct — named placeholders
_logger.LogInformation("Saving action {Id} for user {UserId}", item.Id, userId);

// Wrong — string interpolation defeats structured logging
_logger.LogInformation($"Saving action {item.Id}");
```

### Dependency Injection

- Services registered as `AddScoped` in `Program.cs`
- Constructor injection with `private readonly` fields in `.cs` services
- `@inject` directives in Razor files; property name matches type name (`@inject ApiService ApiService`)
- Interface-based registration used selectively (`IGameDayActionService` -> `GameDayActionService`)

### Razor / Blazor Patterns

- `@attribute [Authorize]` on pages requiring login; `[AllowAnonymous]` on Login
- `@page "/route"` for routing; `{Param:type}` for route parameters
- Lifecycle: prefer `OnInitializedAsync()` for data loading, `OnAfterRenderAsync` for JS interop
- Implement `IDisposable`/`IAsyncDisposable` when subscribing to events
- Use `InvokeAsync(StateHasChanged)` from non-UI thread callbacks
- All UI uses MudBlazor components — no raw HTML form elements
- Dialogs use `IDialogService.ShowAsync<T>()` with typed `DialogParameters<T>`
- Cascading parameters for dialog instances: `[CascadingParameter] private IMudDialogInstance MudDialog`

### Adding a New Page

1. Create `Pages/MyPage.razor` with `@page "/my-route"`
2. Add `@attribute [Authorize]` if login is required
3. Inject services: `@inject ApiService ApiService`
4. Add a nav entry in `Layout/NavMenu.razor` if needed

### Comments

- XML doc comments (`///`) on service methods and interfaces
- Comments in German or English (both are used in the codebase)
- Section separators with `// ─────────` for logical groupings
