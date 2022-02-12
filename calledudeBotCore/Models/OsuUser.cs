using Newtonsoft.Json;
using System.Collections.Generic;

namespace calledudeBot.Models;

public record OsuUser
{
    [JsonConstructor]
    public OsuUser(string username, int pp_rank, string level, float pp_raw, float accuracy, int pp_country_rank, List<object> events)
    {
        Username = username;
        Rank = pp_rank;
        Level = level;
        PP = pp_raw;
        Accuracy = accuracy;
        CountryRank = pp_country_rank;
        Events = events;
    }

    public string Username { get; init; }
    public int Rank { get; init; }
    public string Level { get; init; }
    public float PP { get; init; }
    public float Accuracy { get; init; }
    public int CountryRank { get; init; }
    public List<object> Events { get; init; }
}
