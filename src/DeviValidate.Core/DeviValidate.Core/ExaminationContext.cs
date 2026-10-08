using System;

namespace DeviValidate.Core;

/// <summary>Who ran the tool, on which machine, and when. All fields are optional except the clock and machine name.</summary>
public sealed class ExaminationContext
{
	public string? Examiner { get; init; }

	public string? Agency { get; init; }

	public string? CaseReference { get; init; }

	public string? ValidatedBy { get; init; }

	public string? ValidationDate { get; init; }

	public string? LabProcedure { get; init; }

	public DateTimeOffset Timestamp { get; init; }

	public string MachineName { get; init; } = "";


	public string TimeZoneId { get; init; } = "";


	public static ExaminationContext Capture(string? examiner, string? caseReference, string? validatedBy = null, string? validationDate = null, string? labProcedure = null, string? agency = null)
	{
		return new ExaminationContext
		{
			Examiner = BlankToNull(examiner),
			Agency = BlankToNull(agency),
			CaseReference = BlankToNull(caseReference),
			ValidatedBy = BlankToNull(validatedBy),
			ValidationDate = BlankToNull(validationDate),
			LabProcedure = BlankToNull(labProcedure),
			Timestamp = DateTimeOffset.Now,
			MachineName = Environment.MachineName,
			TimeZoneId = TimeZoneInfo.Local.Id
		};
	}

	public static string? BlankToNull(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		return value.Trim();
	}
}
