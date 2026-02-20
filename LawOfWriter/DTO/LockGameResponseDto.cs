namespace LawOfWriter.DTO;

public class LockGameResponseDto
{
    public string? UserId { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedSince { get; set; }
}