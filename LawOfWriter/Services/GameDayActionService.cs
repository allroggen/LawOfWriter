using LawOfWriter.DTO;

namespace LawOfWriter.Services;

public class GameDayActionService : IGameDayActionService
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;
    private readonly ILogger<GameDayActionService> _logger;

    public GameDayActionService(
        ApiService apiService,
        AuthService authService,
        ILogger<GameDayActionService> logger)
    {
        _apiService = apiService;
        _authService = authService;
        _logger = logger;
    }

    public async Task<bool> SaveGameDayActionAsync(GameDayActionDto item)
    {
        var userId = await _authService.GetUserIdAsync();
        var now = DateTime.UtcNow;

        // Bei neuen Einträgen Created/Createdby setzen
        if (item.Id == 0 || item.Created is null)
        {
            item.Created = now;
            item.Createdby = userId;
        }

        // Changed/Changedby immer setzen
        item.Changed = now;
        item.Changedby = userId;

        _logger.LogInformation(
            "Saving GameDayAction (Id: {Id}) by userId '{UserId}'", item.Id, userId);

        return await _apiService.PostAsync<GameDayActionDto>("data/gameaction", item);
    }
}

