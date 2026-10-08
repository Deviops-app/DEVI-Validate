using System.Globalization;

namespace Devi.Updates;

public static class ByteSize
{
	public static string Format(long bytes) => bytes.ToString("N0", CultureInfo.InvariantCulture) + " bytes";
}
