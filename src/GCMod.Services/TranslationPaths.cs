using System;
using System.IO;

namespace GCMod.Services;

public static class TranslationPaths
{
	public const string Manifest = "manifest";

	public const string Names = "names";

	public const string Master = "master";

	public const string Novels = "novels";

	public static string BuildRemoteUrl(string cdn, string type, string language, string id = null)
	{
		string text = cdn.TrimEnd('/') + "/translations/" + language;
		switch (type)
		{
		case "manifest":
		case "names":
		case "master":
			return text + "/" + type + ".json";
		case "novels":
			if (id != null)
			{
				return text + "/novels/" + id + ".json";
			}
			throw new ArgumentException("Novel ID is required for novels type");
		default:
			throw new ArgumentException("Unknown translation type: " + type);
		}
	}

	public static string BuildCachePath(string cacheDir, string type, string language, string id = null)
	{
		string path = Path.Combine(cacheDir, language);
		switch (type)
		{
		case "manifest":
			return Path.Combine(path, "manifest.json");
		case "names":
			return Path.Combine(path, "names.json");
		case "master":
			return Path.Combine(path, "master.json");
		case "novels":
			if (id != null)
			{
				return Path.Combine(path, "novels", id + ".json");
			}
			throw new ArgumentException("Novel ID is required for novels type");
		default:
			throw new ArgumentException("Unknown translation type: " + type);
		}
	}
}
