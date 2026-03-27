namespace LawOfWriter.Models;

/// <summary>
/// Lokale Kopie eines GameDayAction-Datensatzes, der in der Browser-IndexedDB gespeichert wird.
/// Das Flag IsSynced zeigt an, ob der Datensatz erfolgreich zur API übertragen wurde.
/// </summary>
public class LocalGameDayAction
{
    public int Id { get; set; }
    public int? GameId { get; set; }
    public string? UserId { get; set; }
    public string? UserFullName { get; set; }
    public string? UserNickname { get; set; }
    public string? UserImage { get; set; }
    public int? Pumpe { get; set; }
    public int? Band { get; set; }
    public decimal? Spiele { get; set; }
    public int? Neuner { get; set; }
    public int? Kranz { get; set; }
    public bool? Present { get; set; }
    public bool? IsLocked { get; set; }
    public DateTime? Created { get; set; }
    public string? Createdby { get; set; }
    public DateTime? Changed { get; set; }
    public string? Changedby { get; set; }
    public int? PricelistId { get; set; }
    public bool? IsPresent { get; set; }
    public decimal? Gesamt { get; set; }

    /// <summary>
    /// Gibt an, ob dieser Datensatz bereits erfolgreich zur API synchronisiert wurde.
    /// </summary>
    public bool IsSynced { get; set; } = false;

    /// <summary>
    /// Zeitstempel der letzten Synchronisierung (UTC).
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
}

