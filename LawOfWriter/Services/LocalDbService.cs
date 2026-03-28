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
    private readonly SemaphoreSlim _writeLock = new(1, 1);

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
    /// Falls der Datensatz bereits existiert, wird der bestehende IsSynced-Status beibehalten.
    /// Der isSynced-Parameter wird nur verwendet, wenn der Datensatz neu angelegt wird.
    /// Writes are serialized to prevent sync-status race conditions.
    /// </summary>
    public async Task SaveGameDayActionAsync(GameDayActionDto action, bool isSynced = false)
    {
        await _writeLock.WaitAsync();
        try
        {
            // Bestehenden Sync-Status aus der DB lesen und beibehalten
            var existing = await GetGameDayActionAsync(action.Id);
            var effectiveSynced = existing?.IsSynced ?? isSynced;

            var local = MapToLocal(action, effectiveSynced);
            if (existing?.LastSyncedAt is not null)
                local.LastSyncedAt = existing.LastSyncedAt;

            var json = JsonSerializer.Serialize(local, JsonOptions);
            await _js.InvokeAsync<bool>("localDb.saveGameDayAction", json);
        }
        finally
        {
            _writeLock.Release();
        }
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
    /// Speichert einen geänderten GameDayAction-Datensatz und setzt IsSynced explizit auf false.
    /// Verwenden wenn der User einen Datensatz bearbeitet hat.
    /// </summary>
    public async Task SaveGameDayActionAsUnsyncedAsync(GameDayActionDto action)
    {
        await _writeLock.WaitAsync();
        try
        {
            var local = MapToLocal(action, isSynced: false);
            local.LastSyncedAt = null;
            var json = JsonSerializer.Serialize(local, JsonOptions);
            await _js.InvokeAsync<bool>("localDb.saveGameDayAction", json);
        }
        finally
        {
            _writeLock.Release();
        }
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
    /// Gibt den Sync-Status (Gesamt- und Synced-Anzahl) für einen Spieltag zurück.
    /// </summary>
    public async Task<(int Total, int Synced)> GetSyncStatusAsync(int gameId)
    {
        var actions = await GetActionsByGameIdAsync(gameId);
        return (actions.Count, actions.Count(a => a.IsSynced));
    }

    /// <summary>
    /// Baut einen vollständigen GameApiDto aus den lokal gespeicherten Daten zusammen.
    /// Gibt null zurück wenn der Spieltag lokal nicht vorhanden ist.
    /// </summary>
    public async Task<GameApiDto?> GetLocalGameApiAsync(int gameId)
    {
        var gameDay = await GetGameDayAsync(gameId);
        if (gameDay is null) return null;

        var localActions = await GetActionsByGameIdAsync(gameId);
        var dtos = localActions.Select(MapToDto).ToList();

        return new GameApiDto { GameDayDto = gameDay, GameDayActionDtos = dtos };
    }

    /// <summary>
    /// Gibt alle lokal gespeicherten GameApiDto zurück (alle Spieltage mit ihren Actions).
    /// </summary>
    public async Task<List<GameApiDto>> GetAllLocalGameApisAsync()
    {
        var gameDays = await GetAllGameDaysAsync();
        var result = new List<GameApiDto>();
        foreach (var gd in gameDays)
        {
            var localActions = await GetActionsByGameIdAsync(gd.Id);
            result.Add(new GameApiDto
            {
                GameDayDto = gd,
                GameDayActionDtos = localActions.Select(MapToDto).ToList()
            });
        }
        return result;
    }

    /// <summary>
    /// Löscht alle lokal gespeicherten Daten (GameDays und Actions).
    /// </summary>
    public async Task ClearAllAsync()
    {
        await _js.InvokeAsync<bool>("localDb.clearAll");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DrinkNotes (handschriftliche Getränke-Notizen)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Speichert eine Getränke-Notiz. Bei neuen Notizen (Id == null) wird die
    /// von IndexedDB generierte ID zurückgegeben.
    /// </summary>
    public async Task<int> SaveDrinkNoteAsync(DrinkNote note)
    {
        note.UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(note, JsonOptions);
        var id = await _js.InvokeAsync<int>("localDb.saveDrinkNote", json);
        return id;
    }

    /// <summary>
    /// Lädt eine einzelne Getränke-Notiz anhand der ID.
    /// </summary>
    public async Task<DrinkNote?> GetDrinkNoteAsync(int id)
    {
        var json = await _js.InvokeAsync<string?>("localDb.getDrinkNote", id);
        return json is null ? null : JsonSerializer.Deserialize<DrinkNote>(json, JsonOptions);
    }

    /// <summary>
    /// Gibt alle Getränke-Notizen für einen bestimmten Spieltag zurück.
    /// </summary>
    public async Task<List<DrinkNote>> GetDrinkNotesByGameIdAsync(int gameId)
    {
        var json = await _js.InvokeAsync<string>("localDb.getDrinkNotesByGameId", gameId);
        return JsonSerializer.Deserialize<List<DrinkNote>>(json, JsonOptions) ?? [];
    }

    /// <summary>
    /// Gibt alle lokal gespeicherten Getränke-Notizen zurück.
    /// </summary>
    public async Task<List<DrinkNote>> GetAllDrinkNotesAsync()
    {
        var json = await _js.InvokeAsync<string>("localDb.getAllDrinkNotes");
        return JsonSerializer.Deserialize<List<DrinkNote>>(json, JsonOptions) ?? [];
    }

    /// <summary>
    /// Löscht eine einzelne Getränke-Notiz.
    /// </summary>
    public async Task DeleteDrinkNoteAsync(int id)
    {
        await _js.InvokeAsync<bool>("localDb.deleteDrinkNote", id);
    }

    /// <summary>
    /// Löscht alle Getränke-Notizen.
    /// </summary>
    public async Task ClearDrinkNotesAsync()
    {
        await _js.InvokeAsync<bool>("localDb.clearDrinkNotes");
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
        UserNickname = dto.User?.Nickname,
        UserImage = dto.User?.Iban,
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

    private static GameDayActionDto MapToDto(LocalGameDayAction local) => new()
    {
        Id = local.Id,
        GameId = local.GameId,
        UserId = local.UserId,
        User = local.UserFullName is not null
            ? new AspNetUser
            {
                Id = local.UserId ?? "",
                Name = local.UserFullName,
                Nickname = local.UserNickname,
                Iban = local.UserImage
            }
            : null,
        Pumpe = local.Pumpe,
        Band = local.Band,
        Spiele = local.Spiele,
        Neuner = local.Neuner,
        Kranz = local.Kranz,
        Present = local.Present,
        IsLocked = local.IsLocked,
        Created = local.Created,
        Createdby = local.Createdby,
        Changed = local.Changed,
        Changedby = local.Changedby,
        PricelistId = local.PricelistId,
        IsPresent = local.IsPresent,
        Gesamt = local.Gesamt,
    };
}

