using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Devi.Theme;

namespace DeviValidate.Desktop;

public sealed class ResultPillBackgroundConverter : IValueConverter
{
	private static Brush Match => DeviTheme.Brush("ChipMatchBrush");

	private static Brush Mismatch => DeviTheme.Brush("ChipMismatchBrush");

	private static Brush Missing => DeviTheme.Brush("ChipMissingBrush");

	private static Brush Neutral => DeviTheme.Brush("NeutralSoftBrush");

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		switch (value as string)
		{
		case "Match":
		case "Pass":
			return Match;
		case "Mismatch":
		case "Fail":
			return Mismatch;
		case "Missing":
			return Missing;
		default:
			return Neutral;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

}
