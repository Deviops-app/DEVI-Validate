using System;
using System.Collections.Generic;
using System.IO;

namespace DeviValidate.Core.Expected;

public static class PathKeys
{
	public static string Normalize(string path)
	{
		string text = path.Trim().Trim('"');
		text = text.Replace('\\', '/');
		if (text.StartsWith("./", StringComparison.Ordinal))
		{
			string text2 = text;
			text = text2.Substring(2, text2.Length - 2);
		}
		string text3 = "";
		string text4 = text;
		if (text4.Length >= 2 && char.IsLetter(text4[0]) && text4[1] == ':')
		{
			text3 = char.ToUpperInvariant(text4[0]) + ":";
			string text2 = text4;
			text4 = text2.Substring(2, text2.Length - 2);
		}
		List<string> list = new List<string>();
		string[] array = text4.Split('/', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text5 in array)
		{
			if (!(text5 == "."))
			{
				if (text5 == "..")
				{
					throw new InvalidDataException("Path '" + path + "' contains '..' and cannot be used.");
				}
				list.Add(text5);
			}
		}
		string text6 = string.Join('/', list);
		if (text3.Length == 0)
		{
			return text6;
		}
		return text3 + "/" + text6;
	}

	public static string FileName(string normalizedPath)
	{
		int num = normalizedPath.LastIndexOf('/');
		if (num < 0)
		{
			return normalizedPath;
		}
		int num2 = num + 1;
		return normalizedPath.Substring(num2, normalizedPath.Length - num2);
	}
}
