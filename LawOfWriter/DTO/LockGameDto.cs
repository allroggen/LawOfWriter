namespace LawOfWriter.DTO;

public class LockGameDto
{
    public required int GameId { get; set; }
    public required string UserId { get; set; }
    public DateTime? Changed { get; set; }
}