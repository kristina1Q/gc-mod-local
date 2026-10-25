using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GCMod.Services;

internal static class TranslationHash
{
	private static readonly IComparer<string> KeyComparer = Comparer<string>.Create(CompareKeys);

	public static string Compute(string json)
	{
		using JsonDocument jsonDocument = JsonDocument.Parse(json);
		StringBuilder stringBuilder = new StringBuilder();
		Append(jsonDocument.RootElement, "", stringBuilder);
		return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(stringBuilder.ToString()))).ToLowerInvariant();
	}

	private static void Append(JsonElement table, string prefix, StringBuilder content)
	{
		foreach (JsonProperty item in table.EnumerateObject().OrderBy((JsonProperty property) => property.Name, KeyComparer))
		{
			string text = prefix + item.Name;
			if (item.Value.ValueKind == JsonValueKind.Object)
			{
				Append(item.Value, text + "\u0001", content);
			}
			else
			{
				content.Append(text).Append('\0').Append(item.Value.GetString())
					.Append('\0');
			}
		}
	}

	private static int CompareKeys(string left, string right)
	{
		StringRuneEnumerator enumerator = left.EnumerateRunes().GetEnumerator();
		StringRuneEnumerator enumerator2 = right.EnumerateRunes().GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (!enumerator2.MoveNext())
			{
				return 1;
			}
			int num = enumerator.Current.Value.CompareTo(enumerator2.Current.Value);
			if (num != 0)
			{
				return num;
			}
		}
		if (!enumerator2.MoveNext())
		{
			return 0;
		}
		return -1;
	}
}
