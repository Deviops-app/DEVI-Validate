using System;
using System.Collections.Generic;
using System.IO;

namespace DeviValidate.Core;

/// <summary>
/// Files and folders chosen in one field. Adding keeps what is already there.
/// Removing takes one path off the list. Escape clears this field only.
/// </summary>
public sealed class PathSelection
{
	private readonly List<string> _paths = new List<string>();

	public IReadOnlyList<string> Paths => _paths;

	public bool HasAny => _paths.Count > 0;

	public bool CanClear => _paths.Count > 0;

	/// <summary>Appends paths that are not already in the list. Returns how many were added.</summary>
	public int Add(IEnumerable<string>? paths)
	{
		if (paths == null)
		{
			return 0;
		}
		int added = 0;
		foreach (string path in paths)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				continue;
			}
			if (IndexOf(path) >= 0)
			{
				continue;
			}
			_paths.Add(path);
			added++;
		}
		return added;
	}

	/// <summary>Removes one path. Other paths stay. False when that path was not selected.</summary>
	public bool Remove(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		int index = IndexOf(path);
		if (index < 0)
		{
			return false;
		}
		_paths.RemoveAt(index);
		return true;
	}

	public void Clear()
	{
		_paths.Clear();
	}

	/// <summary>Clears this field. False when it was already empty, so a second Escape does nothing.</summary>
	public bool HandleEscape()
	{
		if (!CanClear)
		{
			return false;
		}
		Clear();
		return true;
	}

	public static string Key(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
		{
			return path;
		}
	}

	public static string ChipName(string path)
	{
		string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string name = Path.GetFileName(trimmed);
		return string.IsNullOrEmpty(name) ? path : name;
	}

	private int IndexOf(string path)
	{
		string key = Key(path);
		for (int i = 0; i < _paths.Count; i++)
		{
			if (string.Equals(Key(_paths[i]), key, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}
}
