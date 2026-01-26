using System.Text.Json.Serialization;

namespace LawOfWriter.Models;

public class RootGame {
    [JsonPropertyName("gameDay")]
    public GameDay gameDay { get; set; }

    [JsonPropertyName("gameAction")]
    public List<GameAction> gameAction { get; set; }
}
// Root myDeserializedClass = JsonSerializer.Deserialize<List<RootGame>>(myJsonResponse);
