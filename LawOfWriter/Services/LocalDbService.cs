using System.Text.Json;
using LawOfWriter.DTO;
using LawOfWriter.Models;
using Microsoft.JSInterop;

namespace LawOfWriter.Services;

/// <summary>
/// Service zum Speichern und Abrufen von Spieltags-Daten in der Browser-IndexedDB.
/// Jeder GameDayAction-Datensatz enthält ein IsSynced-Flag, das nach erfolgreicher
/// API-Übertragung auf true gesetzt werden soll.
/// </summary>
public class LocalDbService
{
    private readonly IJSRuntime _js;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public LocalDbService(IJSRuntime js)
    {
        _js = js;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GameDay
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Speichert einen GameDay in der lokalen Datenbank (Insert oder Update).
    /// </summary>
    public async Task SaveGameDayAsync(GameDayDto gameDay)
    {
        var json = JsonSerializer.Serialize(gameDay, JsonOptions);
        await _js.InvokeAsync<bool>("localDb.saveGameDay", json);
    }

    /// <summary>
    /// Lädt einen GameDay anhand der ID aus der lokalen Datenbank.
    /// </summary>
    public async Task<GameDayDto?> GetGameDayAsync(int id)
    {
        var json = await _js.InvokeAsync<string?>("localDb.getGameDay", id);
        return json is null ? null : JsonSerializer.Deserialize<GameDayDto>(json, JsonOptions);
    }

    /// <summary>
    /// Gibt alle lokal gespeicherten GameDays zurück.
    /// </summary>
    public async Task<List<GameDayDto>> GetAllGameDaysAsync()
    {
        var json = await _js.InvokeAsync<string>("localDb.getAllGameDays");
        return JsonSerializer.Deserialize<List<GameDayDto>>(json, JsonOptions) ?? [];
    }

    /// <summary>
    /// Löscht einen GameDay aus der lokalen Datenbank.
    /// </summary>
    public async Task DeleteGameDayAsync(int id)
    {
        await _js.InvokeAsync<bool>("localDb.deleteGameDay", id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GameDayAction
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Speichert einen GameDayAction-Datensatz in der lokalen Datenbank.
    /// IsSynced wird beim ersten Speichern auf false gesetzt.
    /// </summary>
    public async Task SaveGameDayActionAsync(GameDayActionDto action, bool isSynced = false)
    {
        var local = MapToLocal(action, isSynced);
        var json = JsonSerializer.Serialize(local, JsonOptions);
        await _js.InvokeAsync<bool>("localDb.saveGameDayAction", json);
    }

    /// <summary>
    /// Speichert alle Actions eines kompletten GameApiDto in der lokalen Datenbank.
    /// </summary>
    public async Task SaveGameApiAsync(GameApiDto gameApi, bool isSynced = false)
    {
        await SaveGameDayAsync(gameApi.GameDayDto);

        foreach (var action in gameApi.GameDayActionDtos)
        {
            await SaveGameDayActionAsync(action, isSynced);
        }
    }

    /// <summary>
    /// Lädt einen einzelnen GameDayAction-Datensatz.
    /// </summary>
    public async Task<LocalGameDayAction?> GetGameDayActionAsync(int id)
    {
        var json = await _js.InvokeAsync<string?>("localDb.getGameDayAction", id);
        return json is null ? null : JsonSerializer.Deserialize<LocalGameDayAction>(json, JsonOptions);
    }

    /// <summary>
    /// Gibt alle GameDayActions für einen bestimmten Spieltag zurück.
    /// </summary>
    public async Task<List<LocalGameDayAction>> GetActionsByGameIdAsync(int gameId)
    {
        var json = await _js.InvokeAsync<string>("localDb.getActionsByGameId", gameId);
        return JsonSerializer.Deserialize<List<LocalGameDayAction>>(json, JsonOptions) ?? [];
    }

    /// <summary>
    /// Gibt alle noch nicht synchronisierten GameDayAction-Datensätze zurück.
    /// </summary>
    public async Task<List<LocalGameDayAction>> GetUnsyncedActionsAsync()
    {
        var json = await _js.InvokeAsync<string>("localDb.getUnsyncedActions");
        return JsonSerializer.Deserialize<List<LocalGameDayAction>>(json, JsonOptions) ?? [];
    }

    /// <summary>
    /// Markiert einen einzelnen Datensatz als erfolgreich synchronisiert.
    /// </summary>
    public async Task MarkActionAsSyncedAsync(int id)
    {
        await _js.InvokeAsync<bool>("localDb.markActionAsSynced", id);
    }

    /// <summary>
    /// Markiert alle Actions eines Spieltages als erfolgreich synchronisiert.
    /// </summary>
    public async Task MarkAllActionsAsSyncedAsync(int gameId)
    {
        await _js.InvokeAsync<bool>("localDb.markAllActionsAsSynced", gameId);
    }

    /// <summary>
    /// Löscht einen einzelnen GameDayAction-Datensatz.
    /// </summary>
    public async Task DeleteGameDayActionAsync(int id)
    {
        await _js.InvokeAsync<bool>("localDb.deleteGameDayAction", id);
    }

    /// <summary>
    /// Löscht alle lokal gespeicherten Daten (GameDays und Actions).
    /// </summary>
    public async Task ClearAllAsync()
    {
        await _js.InvokeAsync<bool>("localDb.clearAll");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────────────────────────────────────

    private static LocalGameDayAction MapToLocal(GameDayActionDto dto, bool isSynced) => new()
    {
        Id = dto.Id,
        GameId = dto.GameId,
        UserId = dto.UserId,
        UserFullName = dto.User?.FullName,
        Pumpe = dto.Pumpe,
        Band = dto.Band,
        Spiele = dto.Spiele,
        Neuner = dto.Neuner,
        Kranz = dto.Kranz,
        Present = dto.Present,
        IsLocked = dto.IsLocked,
        Created = dto.Created,
        Createdby = dto.Createdby,
        Changed = dto.Changed,
        Changedby = dto.Changedby,
        PricelistId = dto.PricelistId,
        IsPresent = dto.IsPresent,
        Gesamt = dto.Gesamt,
        IsSynced = isSynced,
        LastSyncedAt = isSynced ? DateTime.UtcNow : null
    };
}

