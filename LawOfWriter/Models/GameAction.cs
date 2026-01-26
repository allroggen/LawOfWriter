using System.Text.Json.Serialization;

namespace LawOfWriter.Models;

public class GameAction {
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("gameId")]
    public int gameId { get; set; }

    [JsonPropertyName("userId")]
    public string userId { get; set; }

    [JsonPropertyName("pumpe")]
    public int pumpe { get; set; }

    [JsonPropertyName("band")]
    public int band { get; set; }

    [JsonPropertyName("spiele")]
    public double spiele { get; set; }

    [JsonPropertyName("neuner")]
    public int neuner { get; set; }

    [JsonPropertyName("kranz")]
    public int kranz { get; set; }

    [JsonPropertyName("present")]
    public object present { get; set; }

    [JsonPropertyName("isLocked")]
    public bool isLocked { get; set; }

    [JsonPropertyName("created")]
    public DateTime created { get; set; }

    [JsonPropertyName("createdby")]
    public string createdby { get; set; }

    [JsonPropertyName("changed")]
    public object changed { get; set; }

    [JsonPropertyName("changedby")]
    public object changedby { get; set; }

    [JsonPropertyName("pricelistId")]
    public int pricelistId { get; set; }

    [JsonPropertyName("isPresent")]
    public bool isPresent { get; set; }

    [JsonPropertyName("gesamt")]
    public double gesamt { get; set; }
}