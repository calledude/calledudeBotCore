using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace calledudeBot.Models;

public record OsuUser
{
    public required string Username { get; init; }

    [JsonPropertyName("pp_rank")]
    public required int Rank { get; init; }
    public required string Level { get; init; }

    [JsonPropertyName("pp_raw")]
    public required float PP { get; init; }
    public required float Accuracy { get; init; }

    [JsonPropertyName("pp_country_rank")]
    public required int CountryRank { get; init; }
    public required List<object>? Events { get; init; }
}
