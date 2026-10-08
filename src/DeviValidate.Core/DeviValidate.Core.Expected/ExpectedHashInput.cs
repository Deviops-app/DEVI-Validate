using System;
using System.Collections.Generic;
using System.Linq;

namespace DeviValidate.Core.Expected;

/// <summary>One chosen hash file or folder in the Expected hash field.</summary>
public sealed class ExpectedHashFile
{
	public ExpectedHashFile(string path, ExpectedHashes? parsed, string? suggestedSource)
	{
		Path = path;
		Parsed = parsed;
		SuggestedSource = suggestedSource;
		ChipName = PathSelection.ChipName(path);
	}

	public string Path { get; }

	public ExpectedHashes? Parsed { get; }

	public string? SuggestedSource { get; }

	public string ChipName { get; }
}

/// <summary>
/// The Expected hash field: any number of chosen files or folders, plus pasted text.
/// Pasted text stays when a file is added, and a file stays when text is pasted.
/// Removing a chip takes only that item. Escape clears this field and nothing else.
/// </summary>
public sealed class ExpectedHashInput
{
	private readonly List<ExpectedHashFile> _files = new List<ExpectedHashFile>();

	/// <summary>The field stays editable so a hash can be typed or pasted beside the chosen files.</summary>
	public bool FieldIsEditable => true;

	public IReadOnlyList<ExpectedHashFile> Files => _files;

	public string PastedText { get; private set; } = "";

	/// <summary>Lab procedure filled in from a chosen file, or null when the examiner owns the value.</summary>
	public string? SuggestedSource { get; private set; }

	/// <summary>True after a comparison that used the current files and text. Cleared when that set changes.</summary>
	public bool HasComparison { get; private set; }

	public bool HasFiles => _files.Count > 0;

	public bool CanClear => HasFiles || PastedText.Length > 0;

	public bool IsEmpty => !HasFiles
		&& PastedText.Length == 0
		&& SuggestedSource == null
		&& !HasComparison;

	/// <summary>
	/// Adds a file or folder. A path that is already selected is refreshed in place.
	/// Other items and any pasted text stay.
	/// </summary>
	/// <returns>True when the path was not already in the list.</returns>
	public bool AddFile(string path, ExpectedHashes? parsed, string? suggestedSource, bool keepExistingSource)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("An expected hash file needs a path.", nameof(path));
		}
		ExpectedHashVerificationInput? before = ForVerification();
		ExpectedHashFile item = new ExpectedHashFile(path, parsed, suggestedSource);
		int index = IndexOf(path);
		bool added = index < 0;
		if (added)
		{
			_files.Add(item);
		}
		else
		{
			_files[index] = item;
		}
		if (!keepExistingSource && string.IsNullOrEmpty(SuggestedSource) && !string.IsNullOrEmpty(suggestedSource))
		{
			SuggestedSource = suggestedSource;
		}
		if (!SameRequest(before, ForVerification()))
		{
			HasComparison = false;
		}
		return added;
	}

	/// <summary>Appends each path. Returns how many were new.</summary>
	public int AddFiles(IEnumerable<string>? paths, Func<string, (ExpectedHashes? Parsed, string? SuggestedSource)>? describe, bool keepExistingSource)
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
			ExpectedHashes? parsed = null;
			string? suggestion = null;
			if (describe != null)
			{
				(parsed, suggestion) = describe(path);
			}
			if (AddFile(path, parsed, suggestion, keepExistingSource))
			{
				added++;
			}
		}
		return added;
	}

	/// <summary>
	/// Stores typed or pasted text. Chosen files stay, so a pasted hash and a hash file can be used together.
	/// </summary>
	public void SetPastedText(string? text)
	{
		string next = text ?? "";
		ExpectedHashVerificationInput? before = ForVerification();
		PastedText = next;
		if (!SameRequest(before, ForVerification()))
		{
			HasComparison = false;
		}
	}

	public void Clear()
	{
		_files.Clear();
		PastedText = "";
		SuggestedSource = null;
		HasComparison = false;
	}

	/// <summary>Removes one chip. Other files and any pasted text stay.</summary>
	public bool RemoveFile(string? path)
	{
		int index = string.IsNullOrWhiteSpace(path) ? -1 : IndexOf(path);
		if (index < 0)
		{
			return false;
		}
		ExpectedHashVerificationInput? before = ForVerification();
		string? removedSuggestion = _files[index].SuggestedSource;
		_files.RemoveAt(index);
		if (!string.IsNullOrEmpty(SuggestedSource) && string.Equals(SuggestedSource, removedSuggestion, StringComparison.Ordinal))
		{
			SuggestedSource = _files.Select((ExpectedHashFile file) => file.SuggestedSource).FirstOrDefault((string? source) => !string.IsNullOrEmpty(source));
		}
		if (!SameRequest(before, ForVerification()))
		{
			HasComparison = false;
		}
		return true;
	}

	/// <summary>Escape clears this field. False when it was already empty.</summary>
	public bool HandleEscape()
	{
		if (!CanClear)
		{
			return false;
		}
		Clear();
		return true;
	}

	public void NoteCompared()
	{
		if (ForVerification() != null)
		{
			HasComparison = true;
		}
	}

	/// <summary>
	/// Files and pasted text verification may use. Null when the field is empty, so nothing earlier is reused.
	/// </summary>
	public ExpectedHashVerificationInput? ForVerification()
	{
		string pasted = PastedText.Trim();
		if (_files.Count == 0 && pasted.Length == 0)
		{
			return null;
		}
		return new ExpectedHashVerificationInput(_files.ToArray(), pasted);
	}

	private int IndexOf(string path)
	{
		string key = PathSelection.Key(path);
		for (int i = 0; i < _files.Count; i++)
		{
			if (string.Equals(PathSelection.Key(_files[i].Path), key, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}

	private static bool SameRequest(ExpectedHashVerificationInput? left, ExpectedHashVerificationInput? right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		if (!string.Equals(left.PastedText, right.PastedText, StringComparison.Ordinal) || left.Files.Count != right.Files.Count)
		{
			return false;
		}
		for (int i = 0; i < left.Files.Count; i++)
		{
			if (!string.Equals(PathSelection.Key(left.Files[i].Path), PathSelection.Key(right.Files[i].Path), StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return true;
	}
}

/// <summary>The files and pasted text a verification is allowed to use.</summary>
public sealed class ExpectedHashVerificationInput
{
	public ExpectedHashVerificationInput(IReadOnlyList<ExpectedHashFile> files, string pastedText)
	{
		Files = files;
		PastedText = pastedText ?? "";
	}

	public IReadOnlyList<ExpectedHashFile> Files { get; }

	public string PastedText { get; }
}
