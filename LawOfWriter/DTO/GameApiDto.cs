namespace LawOfWriter.DTO;

public class GameApiDto
{
    public GameDayDto GameDayDto { get; set; }
    public List<GameDayActionDto> GameDayActionDtos { get; set; }
}