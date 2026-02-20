using LawOfWriter.DTO;

namespace LawOfWriter.Services;

public interface IGameDayActionService
{
    /// <summary>
    /// Speichert eine GameDayAction über die API.
    /// Setzt automatisch Created/Createdby (bei neuen Einträgen) und Changed/Changedby mit dem angemeldeten User.
    /// </summary>
    /// <param name="item">Die zu speichernde GameDayAction</param>
    /// <returns>True wenn erfolgreich, sonst False</returns>
    Task<bool> SaveGameDayActionAsync(GameDayActionDto item);
}

