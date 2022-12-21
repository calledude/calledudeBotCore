
using System.Text.Json.Serialization;

namespace calledudeBot.Models;

public record OsuSong
{
    [JsonPropertyName("version")]
    public required string BeatmapVersion { get; init; }
    public required string Artist { get; init; }
    public required string Title { get; init; }
}
