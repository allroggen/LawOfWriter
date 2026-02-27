namespace LawOfWriter.DTO;

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Vorname { get; set; }
    public DateTime? BDay { get; set; }
    public string? Nickname { get; set; }
    public string? Bild { get; set; }
    public bool IsGuest { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}