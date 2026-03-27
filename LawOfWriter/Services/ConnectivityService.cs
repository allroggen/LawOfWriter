using Microsoft.JSInterop;

namespace LawOfWriter.Services;

/// <summary>
/// Tracks network online/offline status and manages background sync of unsynced local data.
/// Subscribe to StateChanged to be notified when connectivity or sync status changes.
/// </summary>
public class ConnectivityService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly LocalDbService _localDbService;
    private readonly IGameDayActionService _gameDayActionService;
    private readonly ILogger<ConnectivityService> _logger;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;

    public bool IsOnline { get; private set; } = true;
    public bool IsSyncing { get; private set; }
    public int UnsyncedCount { get; private set; }

    public event Action? StateChanged;

    public ConnectivityService(
        IJSRuntime js,
        LocalDbService localDbService,
        IGameDayActionService gameDayActionService,
        ILogger<ConnectivityService> logger)
    {
        _js = js;
        _localDbService = localDbService;
        _gameDayActionService = gameDayActionService;
        _logger = logger;
    }

    /// <summary>
    /// Must be called once from a rendered component (e.g. MainLayout.OnAfterRenderAsync).
    /// Registers online/offline browser event listeners and reads the initial network state.
    /// </summary>
    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        IsOnline = await _js.InvokeAsync<bool>("networkStatus.initialize", _dotNetRef);
        await RefreshUnsyncedCountAsync();
    }

    /// <summary>
    /// Called from JavaScript when the browser's online/offline status changes.
    /// </summary>
    [JSInvokable]
    public void OnNetworkStatusChanged(bool isOnline)
    {
        _logger.LogInformation("Network status changed: {IsOnline}", isOnline);
        IsOnline = isOnline;
        StateChanged?.Invoke();

        if (isOnline)
            _ = SyncAllAsync();
    }

    /// <summary>
    /// Re-counts all unsynced actions in IndexedDB and notifies subscribers.
    /// Call this after any local save or sync operation.
    /// </summary>
    public async Task RefreshUnsyncedCountAsync()
    {
        var unsynced = await _localDbService.GetUnsyncedActionsAsync();
        UnsyncedCount = unsynced.Count;
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Syncs all locally unsynced actions to the API.
    /// No-op when offline or already syncing.
    /// </summary>
    public async Task SyncAllAsync()
    {
        if (!IsOnline || IsSyncing) return;

        IsSyncing = true;
        StateChanged?.Invoke();

        try
        {
            var unsynced = await _localDbService.GetUnsyncedActionsAsync();
            if (unsynced.Count == 0) return;

            var allGameApis = await _localDbService.GetAllLocalGameApisAsync();

            foreach (var action in unsynced)
            {
                var gameApi = allGameApis.FirstOrDefault(g => g.GameDayDto.Id == action.GameId);
                var dto = gameApi?.GameDayActionDtos.FirstOrDefault(d => d.Id == action.Id);
                if (dto is null) continue;

                var synced = false;
                for (var attempt = 0; attempt < 3 && !synced; attempt++)
                {
                    try
                    {
                        if (attempt > 0)
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));

                        var ok = await _gameDayActionService.SaveGameDayActionAsync(dto);
                        if (ok)
                        {
                            await _localDbService.MarkActionAsSyncedAsync(action.Id);
                            _logger.LogInformation("Synced action {Id} (attempt {Attempt})", action.Id, attempt + 1);
                            synced = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Sync attempt {Attempt} failed for action {Id}", attempt + 1, action.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SyncAllAsync");
        }
        finally
        {
            IsSyncing = false;
            await RefreshUnsyncedCountAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef != null)
        {
            try { await _js.InvokeVoidAsync("networkStatus.dispose"); } catch { /* ignore */ }
            _dotNetRef.Dispose();
            _dotNetRef = null;
        }
    }
}
