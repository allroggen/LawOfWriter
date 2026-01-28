namespace LawOfWriter.DTO;

public class GameDayActionDto
{
    public int Id { get; set; }
    public int? GameId { get; set; }
    public string? UserId { get; set; }
    public AspNetUser? User { get; set; }
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
}