using System;
using System.IO;
using System.Security.Cryptography;

namespace DeviValidate.Core;

/// <summary>SHA-256 of the core assembly file, when that file can be read.</summary>
public static class BuildIdentity
{
	public static string? TryCoreHash()
	{
		try
		{
			string location = typeof(ToolInfo).Assembly.Location;
			if (string.IsNullOrEmpty(location) || !File.Exists(location))
			{
				return null;
			}
			using FileStream source = File.OpenRead(location);
			return Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
	}

	/// <summary>SHA-256 of the running executable (the signed DEVI-Validate.exe), when it can be read.</summary>
	public static string? TryExecutableHash(out string? fileName)
	{
		fileName = null;
		try
		{
			string? path = Environment.ProcessPath;
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return null;
			}
			fileName = Path.GetFileName(path);
			using FileStream source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			return Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
	}
}
