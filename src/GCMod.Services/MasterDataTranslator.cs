using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GCMod.Services;

public sealed class MasterDataTranslator
{
	private enum FieldKind
	{
		Text,
		Array,
		Delimited
	}

	private sealed class FieldRule
	{
		public string Name { get; }
		public FieldKind Kind { get; }
		public Dictionary<string, string> Translations { get; }
		public FieldRule(string name, FieldKind kind, Dictionary<string, string> translations)
		{
			Name = name;
			Kind = kind;
			Translations = translations;
		}
	}

	private readonly Dictionary<string, FieldRule[]> _tables = new Dictionary<string, FieldRule[]>(StringComparer.Ordinal);

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public MasterDataTranslator(Dictionary<string, Dictionary<string, Dictionary<string, string>>> tables)
	{
		foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> table in tables)
		{
			table.Deconstruct(out var key, out var value);
			string key2 = key;
			Dictionary<string, Dictionary<string, string>> dictionary = value;
			if (dictionary == null)
			{
				continue;
			}
			List<FieldRule> list = new List<FieldRule>();
			foreach (KeyValuePair<string, Dictionary<string, string>> item in dictionary)
			{
				item.Deconstruct(out key, out var value2);
				string text = key;
				Dictionary<string, string> dictionary2 = value2;
				if (dictionary2 == null)
				{
					continue;
				}
				Dictionary<string, string> dictionary3 = dictionary2.Where((KeyValuePair<string, string> pair) => !string.IsNullOrEmpty(pair.Value) && pair.Value != pair.Key).ToDictionary<KeyValuePair<string, string>, string, string>((KeyValuePair<string, string> pair) => pair.Key, (KeyValuePair<string, string> pair) => pair.Value, StringComparer.Ordinal);
				if (dictionary3.Count != 0)
				{
					FieldKind fieldKind = (text.EndsWith("[]", StringComparison.Ordinal) ? FieldKind.Array : (text.EndsWith('|') ? FieldKind.Delimited : FieldKind.Text));
					string text2;
					switch (fieldKind)
					{
					default:
						text2 = text;
						break;
					case FieldKind.Delimited:
						key = text;
						text2 = key.Substring(0, key.Length - 1);
						break;
					case FieldKind.Array:
						key = text;
						text2 = key.Substring(0, key.Length - 2);
						break;
					}
					string name = text2;
					list.Add(new FieldRule(name, fieldKind, dictionary3));
				}
			}
			if (list.Count > 0)
			{
				_tables.Add(key2, list.ToArray());
			}
		}
	}

	public static bool IsEnabled(string tableName, string[] enabledTables)
	{
		if (tableName != null)
		{
			if (Array.IndexOf(enabledTables, "*") < 0)
			{
				return Array.IndexOf(enabledTables, tableName) >= 0;
			}
			return true;
		}
		return false;
	}

	public string Translate(string tableName, string json, string[] enabledTables)
	{
		if (!IsEnabled(tableName, enabledTables) || !_tables.TryGetValue(tableName, out var value))
		{
			return json;
		}
		JsonNode jsonNode = JsonNode.Parse(json);
		bool flag = false;
		if (jsonNode is JsonArray jsonArray)
		{
			foreach (JsonNode item in jsonArray)
			{
				if (item is JsonObject record)
				{
					flag |= TranslateRecord(record, value);
				}
			}
		}
		else if (jsonNode is JsonObject record2)
		{
			flag = TranslateRecord(record2, value);
		}
		if (!flag)
		{
			return json;
		}
		return jsonNode.ToJsonString(JsonOptions);
	}

	private static bool TranslateRecord(JsonObject record, FieldRule[] fields)
	{
		bool result = false;
		foreach (FieldRule fieldRule in fields)
		{
			bool flag = fieldRule.Kind == FieldKind.Array;
			JsonNode jsonNode = record[fieldRule.Name];
			JsonArray jsonArray = (flag ? (jsonNode as JsonArray) : null);
			if (flag)
			{
				jsonNode = ((jsonArray != null && jsonArray.Count > 0) ? jsonArray[0] : null);
			}
			if (!(jsonNode is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value) || value == null)
			{
				continue;
			}
			int num = ((fieldRule.Kind == FieldKind.Delimited) ? value.IndexOf('|') : (-1));
			string key = ((num < 0) ? value : value.Substring(0, num));
			if (!fieldRule.Translations.TryGetValue(key, out var value2))
			{
				continue;
			}
			if (flag)
			{
				jsonArray[0] = value2;
			}
			else
			{
				string name = fieldRule.Name;
				string text3;
				if (num >= 0)
				{
					string text = value2;
					string text2 = value;
					int num2 = num;
					text3 = text + text2.Substring(num2, text2.Length - num2);
				}
				else
				{
					text3 = value2;
				}
				record[name] = text3;
			}
			result = true;
		}
		return result;
	}
}
