using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Devi.Theme;

namespace DeviValidate.Desktop;

public sealed class ResultPillForegroundConverter : IValueConverter
{
	private static Brush Match => DeviTheme.Brush("SuccessBrush");

	private static Brush Mismatch => DeviTheme.Brush("DangerBrush");

	private static Brush Missing => DeviTheme.Brush("WarningBrush");

	private static Brush Extra => DeviTheme.Brush("SecondaryBrush");

	private static Brush Text => DeviTheme.Brush("TextBrush");

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
		case "Extra":
			return Extra;
		default:
			return Text;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

}
