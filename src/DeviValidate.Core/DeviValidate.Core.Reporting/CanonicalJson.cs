using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeviValidate.Core.Reporting;

/// <summary>
/// JSON encoding used for records on disk and for the integrity hash.
/// Compact encoding is the canonical form. Pretty encoding is for reading.
/// </summary>
public static class CanonicalJson
{
	private sealed class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
	{
		public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return DateTimeOffset.Parse(reader.GetString() ?? throw new JsonException("A timestamp was null."), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
		}

		public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
		}
	}

	public static JsonSerializerOptions Compact { get; } = Create(indented: false);


	public static JsonSerializerOptions Pretty { get; } = Create(indented: true);


	public static string ToCompact<T>(T value)
	{
		return JsonSerializer.Serialize(value, Compact);
	}

	public static string ToPretty<T>(T value)
	{
		return JsonSerializer.Serialize(value, Pretty);
	}

	public static byte[] ToCompactUtf8<T>(T value)
	{
		return JsonSerializer.SerializeToUtf8Bytes(value, Compact);
	}

	public static T FromJson<T>(string json)
	{
		try
		{
			T val = JsonSerializer.Deserialize<T>(json, Pretty);
			if (val == null)
			{
				throw new InvalidDataException("JSON did not contain a record.");
			}
			return val;
		}
		catch (JsonException ex)
		{
			throw new InvalidDataException("Could not read the JSON record: " + ex.Message, ex);
		}
	}

	private static JsonSerializerOptions Create(bool indented)
	{
		return new JsonSerializerOptions
		{
			WriteIndented = indented,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			PropertyNameCaseInsensitive = false,
			UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
			Converters = { (JsonConverter)new DateTimeOffsetConverter() }
		};
	}
}
