using System;
using System.Collections.Generic;
using System.Linq;

namespace DeviValidate.Core.Expected.Vendor;

public static class VendorParserRegistry
{
	private static readonly object Gate = new object();

	private static readonly List<IVendorReportParser> Parsers = new List<IVendorReportParser>();

	private static bool _builtInsRegistered;

	public static IReadOnlyList<IVendorReportParser> All
	{
		get
		{
			lock (Gate)
			{
				return Parsers.ToArray();
			}
		}
	}

	public static void Register(IVendorReportParser parser)
	{
		IVendorReportParser parser2 = parser;
		ArgumentNullException.ThrowIfNull(parser2, "parser");
		lock (Gate)
		{
			if (!Parsers.Any((IVendorReportParser existing) => string.Equals(existing.Name, parser2.Name, StringComparison.Ordinal)))
			{
				Parsers.Add(parser2);
			}
		}
	}

	public static void RegisterBuiltIns()
	{
		lock (Gate)
		{
			if (!_builtInsRegistered)
			{
				_builtInsRegistered = true;
			}
		}
	}
}
