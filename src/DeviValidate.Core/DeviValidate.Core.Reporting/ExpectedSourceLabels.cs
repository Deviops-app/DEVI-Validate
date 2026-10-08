namespace DeviValidate.Core.Reporting;

/// <summary>Plain names for the place the expected hashes came from.</summary>
public static class ExpectedSourceLabels
{
	public static string Describe(string? formatName)
	{
		return formatName switch
		{
			"FTK Imager text log" => "FTK Imager log", 
			"GNU sum" => "sum file", 
			"BSD sum" => "sum file", 
			"bare hash" => "sum file", 
			"CSV" => "CSV", 
			"TSV" => "TSV", 
			"DEVI Validate manifest" => "DEVI Validate manifest", 
			"DEVI Validate verification record" => "DEVI Validate verification record", 
			"single hash" => "entered manually", 
			_ => ReportCopy.OrNotRecorded(formatName), 
		};
	}
}
