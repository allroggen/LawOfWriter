namespace LawOfWriter.Models;

/// <summary>
/// Lokale Getränke-Notiz, die als Handschrift auf dem Canvas erstellt
/// und in der Browser-IndexedDB gespeichert wird.
/// </summary>
public class DrinkNote
{
    /// <summary>Auto-increment ID von IndexedDB.</summary>
    public int? Id { get; set; }

    /// <summary>Verknüpfung zum Spieltag.</summary>
    public int? GameId { get; set; }

    /// <summary>JSON-String der Stroke-Daten (Punkte, Farbe, Breite).</summary>
    public string StrokeData { get; set; } = "[]";

    /// <summary>Erstellungszeitpunkt (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Letzter Änderungszeitpunkt (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Optionaler Name / Beschreibung der Notiz.</summary>
    public string? Label { get; set; }
}
