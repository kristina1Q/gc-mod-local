using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GCMod;

public class Manifest
{
	[JsonPropertyName("hash")]
	public string Hash { get; set; }

	[JsonPropertyName("names")]
	public string Names { get; set; }

	[JsonPropertyName("master")]
	public string Master { get; set; }

	[JsonPropertyName("novels")]
	public Dictionary<string, string> Novels { get; set; }
}
