using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DeviValidate.Core;

namespace DeviValidate.Desktop;

/// <summary>
/// Plain-text diagnostic log under %LocalAppData%\DEVI\Validate\logs.
/// Never throws: logging must not be the reason the app closes.
/// The log holds app/OS details and exception text only, never evidence content or hashes.
/// </summary>
internal static class CrashLog
{
	private static readonly object Gate = new object();

	public static string Directory { get; } = ResolveDirectory();

	public static string FilePath { get; } = Path.Combine(Directory, "validate-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");

	private static string ResolveDirectory()
	{
		try
		{
			string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (!string.IsNullOrEmpty(local))
			{
				return Path.Combine(local, "DEVI", "Validate", "logs");
			}
		}
		catch
		{
		}
		return Path.Combine(Path.GetTempPath(), "DEVI-Validate-logs");
	}

	public static void Startup()
	{
		Write("INFO", "Start DEVI Validate " + ToolInfo.Version
			+ " | OS " + SafeOs()
			+ " | " + RuntimeInformation.FrameworkDescription
			+ " | " + RuntimeInformation.ProcessArchitecture
			+ " | exe " + SafeProcessPath()
			+ " | base " + AppContext.BaseDirectory
			+ " | cwd " + SafeCwd());
	}

	public static void Info(string message)
	{
		Write("INFO", message);
	}

	public static void Error(string where, Exception? exception)
	{
		Write("ERROR", where + Environment.NewLine + (exception?.ToString() ?? "(no exception object)"));
	}

	private static void Write(string level, string message)
	{
		try
		{
			lock (Gate)
			{
				System.IO.Directory.CreateDirectory(Directory);
				string line = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture) + " [" + level + "] " + message + Environment.NewLine;
				File.AppendAllText(FilePath, line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
		catch
		{
		}
	}

	private static string SafeOs()
	{
		try
		{
			return RuntimeInformation.OSDescription + " (" + Environment.OSVersion.VersionString + ")";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeProcessPath()
	{
		try
		{
			return Environment.ProcessPath ?? "unknown";
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeCwd()
	{
		try
		{
			return Environment.CurrentDirectory;
		}
		catch
		{
			return "unknown";
		}
	}
}
