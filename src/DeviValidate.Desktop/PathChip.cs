namespace DeviValidate.Desktop;

/// <summary>One selected file or folder, shown as a chip.</summary>
public sealed class PathChip
{
	public PathChip(string path, string removeName, string chipName)
	{
		Path = path;
		Name = DeviValidate.Core.PathSelection.ChipName(path);
		RemoveName = removeName + " " + Name;
		ChipName = chipName + " " + Name;
		ToolTip = path;
	}

	public string Path { get; }

	public string Name { get; }

	public string RemoveName { get; }

	public string ChipName { get; }

	public string ToolTip { get; }
}
