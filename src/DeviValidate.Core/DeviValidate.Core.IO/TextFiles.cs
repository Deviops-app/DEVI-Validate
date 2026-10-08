using System.IO;
using System.Text;

namespace DeviValidate.Core.IO;

public static class TextFiles
{
	private static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	public static void WriteOutside(string? evidencePath, string destination, string content)
	{
		Guard(evidencePath, destination);
		string text = destination + ".tmp";
		bool flag = false;
		try
		{
			string directoryName = Path.GetDirectoryName(Path.GetFullPath(destination));
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(text, content, Utf8);
			File.Move(text, destination, overwrite: false);
			flag = true;
		}
		finally
		{
			if (!flag && File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	public static void WriteBytesOutside(string? evidencePath, string destination, byte[] content)
	{
		Guard(evidencePath, destination);
		string text = destination + ".tmp";
		bool flag = false;
		try
		{
			string directoryName = Path.GetDirectoryName(Path.GetFullPath(destination));
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllBytes(text, content);
			File.Move(text, destination, overwrite: false);
			flag = true;
		}
		finally
		{
			if (!flag && File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	private static void Guard(string? evidencePath, string destination)
	{
		if (!string.IsNullOrWhiteSpace(evidencePath))
		{
			OutputPathGuard.EnsureOutside(evidencePath, destination);
		}
		if (File.Exists(destination))
		{
			throw new IOException("'" + destination + "' already exists. DEVI Validate does not overwrite reports.");
		}
	}
}
