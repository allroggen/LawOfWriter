namespace LawOfWriter.DTO;

public class GameDayDto
{
    public int Id { get; set; }
    public DateTime GameDay1 { get; set; }
    public bool? IsLocked { get; set; }
    public DateTime? Created { get; set; }
    public string? Createdby { get; set; }
    public DateTime? Changed { get; set; }
    public string? Changedby { get; set; }
    public bool? IsDummy { get; set; }
}