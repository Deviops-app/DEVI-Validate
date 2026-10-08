using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace DeviValidate.Core.Reporting;

/// <summary>
/// Line breaks for the verification table. Paths break on a separator.
/// A size stays intact, or the unit drops to the next line.
/// </summary>
internal static class TableWrap
{
	public static List<string> WrapPath(string text, double width, Func<string, double> measure)
	{
		string path = (string.IsNullOrEmpty(text) ? "-" : text);
		List<string> list = new List<string>();
		string text2 = "";
		foreach (string item in Segments(path))
		{
			if (item.Length == 0)
			{
				continue;
			}
			string text3 = text2 + item;
			if (Fits(text3, width, measure))
			{
				text2 = text3;
				continue;
			}
			if (text2.Length > 0)
			{
				list.Add(text2);
				text2 = "";
			}
			if (Fits(item, width, measure))
			{
				text2 = item;
				continue;
			}
			string text4 = item;
			while (text4.Length > 0)
			{
				int num = LongestFit(text4, width, measure);
				if (num >= text4.Length)
				{
					text2 = text4;
					break;
				}
				if (num <= 0)
				{
					num = 1;
				}
				list.Add(text4.Substring(0, num));
				string text5 = text4;
				int num2 = num;
				text4 = text5.Substring(num2, text5.Length - num2);
			}
		}
		if (text2.Length > 0)
		{
			list.Add(text2);
		}
		if (list.Count == 0)
		{
			list.Add("");
		}
		return list;
	}

	public static List<string> SizeLines(string text, double width, Func<string, double> measure)
	{
		string text2 = (string.IsNullOrEmpty(text) ? "-" : text);
		Span<string> span;
		int num;
		if (Fits(text2, width, measure))
		{
			List<string> list = new List<string>();
			CollectionsMarshal.SetCount(list, 1);
			span = CollectionsMarshal.AsSpan(list);
			num = 0;
			span[num] = text2;
			num++;
			return list;
		}
		int num2 = text2.LastIndexOf(' ');
		if (num2 > 0)
		{
			List<string> list2 = new List<string>();
			CollectionsMarshal.SetCount(list2, 2);
			span = CollectionsMarshal.AsSpan(list2);
			num = 0;
			span[num] = text2.Substring(0, num2);
			num++;
			ref string reference = ref span[num];
			string text3 = text2;
			int num3 = num2 + 1;
			reference = text3.Substring(num3, text3.Length - num3);
			num++;
			return list2;
		}
		List<string> list3 = new List<string>();
		CollectionsMarshal.SetCount(list3, 1);
		span = CollectionsMarshal.AsSpan(list3);
		num = 0;
		span[num] = text2;
		num++;
		return list3;
	}

	public static string PathHtml(string? path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return WebUtility.HtmlEncode(path ?? "");
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < path.Length; i++)
		{
			char c = path[i];
			stringBuilder.Append(WebUtility.HtmlEncode(c.ToString()));
			bool flag;
			switch (c)
			{
			case '-':
			case '.':
			case '/':
			case '\\':
			case '_':
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				stringBuilder.Append("<wbr>");
			}
		}
		return stringBuilder.ToString();
	}

	private static IEnumerable<string> Segments(string path)
	{
		int num = 0;
		for (int index = 0; index < path.Length; index++)
		{
			char c = path[index];
			if ((c == '/' || c == '\\') ? true : false)
			{
				int num2 = num;
				yield return path.Substring(num2, index + 1 - num2);
				num = index + 1;
			}
		}
		if (num < path.Length || path.Length == 0)
		{
			int num2 = num;
			yield return path.Substring(num2, path.Length - num2);
		}
	}

	private static int LongestFit(string text, double width, Func<string, double> measure)
	{
		int num = 0;
		int num2 = 0;
		string text2 = "";
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			text2 += c;
			if (!Fits(text2, width, measure))
			{
				break;
			}
			num2 = text2.Length;
			if ((c == '-' || c == '.' || c == '_') ? true : false)
			{
				num = text2.Length;
			}
		}
		if (num2 == text.Length)
		{
			return text.Length;
		}
		if (num <= 0)
		{
			return num2;
		}
		return num;
	}

	private static bool Fits(string text, double width, Func<string, double> measure)
	{
		return measure(text) <= width;
	}
}
