using System.Text.Json.Serialization;

namespace LawOfWriter.Models;

public class GameDay {
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("gameDay1")]
    public DateTime gameDay1 { get; set; }

    [JsonPropertyName("isLocked")]
    public bool isLocked { get; set; }

    [JsonPropertyName("created")]
    public DateTime created { get; set; }

    [JsonPropertyName("createdby")]
    public string? createdby { get; set; }

    [JsonPropertyName("changed")]
    public object? changed { get; set; }

    [JsonPropertyName("changedby")]
    public object? changedby { get; set; }

    [JsonPropertyName("isDummy")]
    public bool isDummy { get; set; }

    [JsonPropertyName("games")]
    public List<GameAction> games { get; set; } = [];
}