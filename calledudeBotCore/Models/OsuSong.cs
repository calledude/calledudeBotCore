using Newtonsoft.Json;

namespace calledudeBot.Models;

public record OsuSong
{
    [JsonConstructor]
    public OsuSong(string version, string artist, string title)
    {
        BeatmapVersion = version;
        Artist = artist;
        Title = title;
    }

    public string BeatmapVersion { get; init; }
    public string Artist { get; init; }
    public string Title { get; init; }
}
