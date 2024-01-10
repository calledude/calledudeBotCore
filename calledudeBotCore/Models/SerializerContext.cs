using System.Text.Json;
using System.Text.Json.Serialization;

namespace calledudeBot.Models;

[JsonSerializable(typeof(OsuUser[]))]
[JsonSerializable(typeof(OsuSong[]))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class SerializerContext : JsonSerializerContext
{
	public static SerializerContext CaseInsensitive { get; } = new SerializerContext(new JsonSerializerOptions()
	{
		PropertyNameCaseInsensitive = true
	});
}