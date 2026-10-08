using System;
using System.Collections.Generic;
using System.Linq;

namespace DeviValidate.Cli;

internal sealed class ArgList
{
	private static readonly HashSet<string> Flags = new HashSet<string>(StringComparer.Ordinal) { "help", "version", "quiet", "ignore-path-case", "portable" };

	private readonly Dictionary<string, string> _options = new Dictionary<string, string>(StringComparer.Ordinal);

	public List<string> Positionals { get; } = new List<string>();


	public ArgList(IEnumerable<string> args)
	{
		List<string> list = args.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (text == "--")
			{
				Positionals.AddRange(list.Skip(i + 1));
				break;
			}
			if (text.StartsWith("--", StringComparison.Ordinal))
			{
				string text2 = text;
				string text3 = text2.Substring(2, text2.Length - 2);
				if (text3.Length == 0)
				{
					throw new CliException("Missing option name.");
				}
				int num = text3.IndexOf('=');
				string text4;
				string text5;
				if (num >= 0)
				{
					text4 = text3.Substring(0, num);
					text2 = text3;
					int num2 = num + 1;
					text5 = text2.Substring(num2, text2.Length - num2);
				}
				else if (Flags.Contains(text3))
				{
					text4 = text3;
					text5 = "true";
				}
				else
				{
					text4 = text3;
					if (i + 1 >= list.Count || list[i + 1].StartsWith('-'))
					{
						throw new CliException("Missing value for --" + text4 + ".");
					}
					text5 = list[++i];
				}
				_options[text4] = text5 ?? "";
			}
			else if (text.StartsWith('-') && text.Length == 2)
			{
				string text6 = text[1] switch
				{
					'h' => "help", 
					'a' => "algorithm", 
					'o' => "output", 
					'e' => "expected", 
					'd' => "output-dir", 
					_ => throw new CliException("Unknown option " + text + "."), 
				};
				if (Flags.Contains(text6))
				{
					_options[text6] = "true";
					continue;
				}
				if (i + 1 >= list.Count || list[i + 1].StartsWith('-'))
				{
					throw new CliException("Missing value for " + text + ".");
				}
				_options[text6] = list[++i];
			}
			else
			{
				if (text.StartsWith('-'))
				{
					throw new CliException("Unknown option " + text + ".");
				}
				Positionals.Add(text);
			}
		}
	}

	public bool Has(string name)
	{
		return _options.ContainsKey(name);
	}

	public string? Get(string name)
	{
		if (!_options.TryGetValue(name, out string value))
		{
			return null;
		}
		return value;
	}

	public void RequireKnown(params string[] names)
	{
		HashSet<string> hashSet = new HashSet<string>(names, StringComparer.Ordinal);
		foreach (string key in _options.Keys)
		{
			if (!hashSet.Contains(key))
			{
				throw new CliException("Unknown option --" + key + ".");
			}
		}
	}
}
