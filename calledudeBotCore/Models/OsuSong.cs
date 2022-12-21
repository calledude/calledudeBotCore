
using System.Text.Json.Serialization;

namespace calledudeBot.Models;

public record OsuSong
{
    [JsonPropertyName("version")]
    public string BeatmapVersion { get; init; }
    public string Artist { get; init; }
    public string Title { get; init; }
}
