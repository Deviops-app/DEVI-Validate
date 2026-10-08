using System;
using System.Globalization;

namespace DeviValidate.Core;

public static class TimeDisplay
{
	public static string Format(DateTimeOffset value, string? timeZoneId)
	{
		return Format(value, Resolve(timeZoneId));
	}

	public static string Format(DateTimeOffset value, TimeZoneInfo? zone)
	{
		string text = value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
		if (zone == null)
		{
			if (value.Offset == TimeSpan.Zero)
			{
				return text;
			}
			return text + " and " + value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " " + OffsetLabel(value.Offset);
		}
		DateTimeOffset local = TimeZoneInfo.ConvertTime(value, zone);
		string text2 = local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " " + ZoneLabel(zone, local);
		if (string.Equals(text2, text, StringComparison.Ordinal))
		{
			return text;
		}
		return text + " and " + text2;
	}

	private static TimeZoneInfo? Resolve(string? timeZoneId)
	{
		if (string.IsNullOrWhiteSpace(timeZoneId))
		{
			return null;
		}
		if (string.Equals(timeZoneId, "UTC", StringComparison.OrdinalIgnoreCase) || string.Equals(timeZoneId, "Etc/UTC", StringComparison.OrdinalIgnoreCase) || string.Equals(timeZoneId, "Etc/Zulu", StringComparison.OrdinalIgnoreCase))
		{
			return TimeZoneInfo.Utc;
		}
		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
		}
		catch (TimeZoneNotFoundException)
		{
			return null;
		}
		catch (InvalidTimeZoneException)
		{
			return null;
		}
	}

	private static string ZoneLabel(TimeZoneInfo zone, DateTimeOffset local)
	{
		if (zone.Equals(TimeZoneInfo.Utc))
		{
			return "UTC";
		}
		string text = (zone.IsDaylightSavingTime(local) ? zone.DaylightName : zone.StandardName);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = zone.Id;
		}
		return text.Trim();
	}

	private static string OffsetLabel(TimeSpan offset)
	{
		string text = ((offset < TimeSpan.Zero) ? "-" : "+");
		return "UTC" + text + offset.Duration().ToString("hh\\:mm", CultureInfo.InvariantCulture);
	}
}
