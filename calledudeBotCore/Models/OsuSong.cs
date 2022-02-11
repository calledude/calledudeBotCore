﻿using Newtonsoft.Json;

namespace calledudeBot.Models;

public class OsuSong
{
    [JsonProperty("version")]
    public string BeatmapVersion { get; set; }

    [JsonProperty("artist")]
    public string Artist { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }
}
