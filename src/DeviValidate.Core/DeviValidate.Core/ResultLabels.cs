namespace DeviValidate.Core;

/// <summary>Factual result words. The tool describes what it observed and does not assign a cause.</summary>
public static class ResultLabels
{
	public const string Match = "match";

	public const string Mismatch = "mismatch";

	public const string Missing = "missing";

	public const string Extra = "extra";

	public const string AllMatchVerdict = "All files match the expected hashes.";

	public const string ExtrasVerdict = "All expected files match. Additional files were found that are not in the expected list.";

	public const string MismatchVerdict = "One or more files do not match the expected hashes.";

	public const string MissingVerdict = "One or more expected files were not found.";

	public const string MismatchAndMissingVerdict = "One or more files do not match the expected hashes, and one or more expected files were not found.";

	public static string Display(string result)
	{
		return result switch
		{
			"match" => "Match", 
			"mismatch" => "Mismatch", 
			"missing" => "Missing", 
			"extra" => "Extra", 
			_ => result, 
		};
	}

	public static string Verdict(int mismatch, int missing, int extra)
	{
		if (mismatch > 0 && missing > 0)
		{
			return "One or more files do not match the expected hashes, and one or more expected files were not found.";
		}
		if (mismatch > 0)
		{
			return "One or more files do not match the expected hashes.";
		}
		if (missing > 0)
		{
			return "One or more expected files were not found.";
		}
		if (extra > 0)
		{
			return "All expected files match. Additional files were found that are not in the expected list.";
		}
		return "All files match the expected hashes.";
	}

	public static int Rank(string result)
	{
		return result switch
		{
			"mismatch" => 0, 
			"missing" => 1, 
			"extra" => 2, 
			"match" => 3, 
			_ => 4, 
		};
	}
}
