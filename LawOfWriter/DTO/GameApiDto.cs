namespace LawOfWriter.DTO;

public class GameApiDto
{
    public GameDayDto GameDayDto { get; set; } = null!;
    public List<GameDayActionDto> GameDayActionDtos { get; set; } = [];
}