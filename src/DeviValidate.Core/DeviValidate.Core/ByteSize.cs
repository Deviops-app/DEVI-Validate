using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DeviValidate.Core;

public static class ByteSize
{
	public static string Format(long bytes)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(6, 1, invariantCulture);
		handler.AppendFormatted(bytes, "N0");
		handler.AppendLiteral(" bytes");
		return string.Create(invariantCulture, ref handler);
	}
}
