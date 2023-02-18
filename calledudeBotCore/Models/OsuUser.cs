using System.Text.Json.Serialization;

namespace calledudeBot.Models;

[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public record OsuUser
{
	public string Username { get; set; } = null!;

	[JsonPropertyName("pp_rank")]
	public int Rank { get; set; }

	public string Level { get; set; } = null!;

	[JsonPropertyName("pp_raw")]
	public float PP { get; set; }

	[JsonPropertyName("accuracy")]
	public float Accuracy { get; set; }

	[JsonPropertyName("pp_country_rank")]
	public int CountryRank { get; set; }
}
