
using System.Text.Json.Serialization;

namespace calledudeBot.Models;

public record OsuSong
{
    [JsonPropertyName("version")]
    public string BeatmapVersion { get; set; } = null!;
    public string Artist { get; set; } = null!;
    public string Title { get; set; } = null!;
}
