using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeviValidate.Core.Reporting;

/// <summary>Wording shared by the HTML record, the PDF record, and the self-test record.</summary>
public static class ReportCopy
{
	public const string Independent = "This record independently recomputes hash values and compares them with values recorded by another tool or process.";

	public const string RedMeans = "Red means the new hash is not the same as the hash written down before. The file we read is not the same as the file that hash describes. The notes below say what that can and cannot mean.";

	public const string MatchMeans = "A match means the new hash is the same as the hash written down before. The file we read is the same file that hash describes. Anyone can check the file again with sha256sum, certutil -hashfile, or Get-FileHash. This check verifies the bytes only. It does not show who made the file, what the file means, or whether a chain of custody is complete.";

	public const string NotRecorded = "Not recorded";

	public const string SetHashNote = "The set hash is the named algorithm applied to the sorted list of relative path, tab, and lowercase hash for each file that was read. Exported files use this set hash as the file name.";

	public const string SingleFileNameNote = "Exported files use the computed hash of this file as the file name.";

	public const string SelfTestIntro = "This is a tool validation record from the built-in self-test. It is not an evidence verification. The checks hash published test vectors for SHA-256, SHA-1, and MD5, and they confirm that a synthetic file's contents and timestamps were unchanged after hashing. A passing result means this build reproduced the published values and the read-only check on this machine. It does not certify the tool for casework.";

	public const string PassedResult = "All self-test checks passed.";

	public const string FailedResult = "One or more self-test checks did not pass.";

	public static IReadOnlyList<ReportSection> Explanation { get; } = System.Array.AsReadOnly(new ReportSection[7]
	{
		new ReportSection("What this record is", "This record independently recomputes hash values and compares them with values recorded by another tool or process. It is a hash verification record from DEVI Validate, an independent, third-party tool. The expected hashes came from the source named on the first page."),
		new ReportSection("What you do to verify", "A verification needs two things: the file or folder, and a hash that another tool already wrote down. DEVI Validate reads the file and computes a new hash. It then compares that new hash with the earlier one. It does not image a drive. It does not connect to a physical analyzer, a forensic suite, or FTK Imager. If that other tool already printed a hash, bring the hash here. You do not run the acquisition again inside this program.\n\nGet the earlier hash from the report you already have. That may be the hash list or acquisition report from a physical analyzer or other forensic tool, an FTK Imager text log, a sum file, a CSV or TSV, a DEVI Validate JSON record, or one hash copied from a report and pasted for a single file. Use the same algorithm the first tool used. SHA-256, SHA-1, and MD5 are different, and a SHA-256 result will not match an MD5 result for the same file. For an FTK Imager log, the checksum is the hash of the acquired data. Compare it with the single raw image, not with an E01 or L01 container. This version does not read a hash stored inside an E01 file.\n\nPress Verify only after both the evidence and the earlier hash are in place. Hash only computes a new hash and does not compare it with anything, so Hash only is not a verification. Match means the new hash is the same as the hash written down before. The file we read is the same file that hash describes. Mismatch, shown in red, means the two hashes are not the same. A red result can mean the file changed, that this is a different file, that the hash was copied wrong, that the algorithm does not match, or that the tools were pointed at different copies, including hashing a container when the first hash was of the raw data. A red result does not show who changed the file, when it changed, or why. It does not show that someone tampered with the file or deleted it.\n\nA prosecutor, a defense attorney, or another examiner can repeat the check without this program. On Windows, use certutil -hashfile with SHA256, or Get-FileHash -Algorithm SHA256. On many other systems, use sha256sum. Use SHA1 or MD5 when that is the algorithm on the first report. Compare the hex values. This check verifies the bytes only. It does not show who made the file, what the file means, or whether a chain of custody is complete. It does not certify the tool, the laboratory, or the case."),
		new ReportSection("How it works", "A cryptographic hash is a digital fingerprint of a file. The same bytes always produce the same hash. A different byte produces a different hash. DEVI Validate read the files and did not modify them. It used the algorithm named on this record. It did not use a network connection.\n\nFor a folder, the set hash is that same algorithm applied to a canonical list of the files that were read. Each line is the relative path, a tab, the lowercase hash, and a line feed. The lines are sorted by path. The same files in any order produce the same set hash. Paths that were expected but not read are not part of the set hash."),
		new ReportSection("What the results mean", "Match. The new hash is the same as the hash written down before. The file we read is the same as the file that hash describes. Match is not shown in red.\n\nMismatch, shown in red. The new hash is not the same as the hash written down before. The file we read is not the same as the file that hash describes.\n\nA red result can mean a few ordinary things. The file may have changed after the first hash was made. This may be a different file. The expected hash may have been copied wrong, or taken from another file. The first tool and this tool may have been pointed at different copies.\n\nA red result does not show who changed the file, when it changed, or why. It does not show that someone tampered with the file or deleted it. It only shows that the two hashes are not the same.\n\nMissing. A path was on the expected list, but that file was not among the files we read. We could not check its hash. Missing is not shown in red.\n\nExtra. We read a file that was not on the expected list. Its hash is here, but there was no earlier hash to compare it with. Extra is not shown in red."),
		new ReportSection("How to independently verify", "Anyone can recompute the file hashes with a standard tool and compare them with this record. Examples include sha256sum, certutil -hashfile, and Get-FileHash. To check this record itself, recompute the SHA-256 of its canonical JSON with the integrity hash set to an empty string, or run devi-validate check on the JSON file. The record hash covers the JSON record. It does not cover the HTML or PDF bytes."),
		new ReportSection("Validation and use", "DEVI Validate is open source. Its built-in self-test uses published test vectors. Each lab or agency should validate it under its own procedures before relying on it in casework. This record does not certify the tool, and it is not a statement of court acceptance, NIST approval, or CJIS compliance.\n\nThe fields below show what was entered when the tool was run."),
		new ReportSection("Limits", "This tool does not check what a file means, whether metadata is accurate, or whether a chain of custody is complete.")
	});


	public static string OrNotRecorded(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return "Not recorded";
	}

	public static string SkippedSummary(int count)
	{
		string text = count.ToString(CultureInfo.InvariantCulture);
		if (count != 1)
		{
			return "Skipped " + text + " paths. They were not hashed.";
		}
		return "Skipped 1 path. It was not hashed.";
	}

	public static string SkippedDetail(SkippedEntry entry)
	{
		if (string.Equals(entry.Reason, "symbolic link", StringComparison.Ordinal))
		{
			return "Symbolic link. It was not followed.";
		}
		if (!string.IsNullOrWhiteSpace(entry.Reason))
		{
			return entry.Reason;
		}
		return "Not hashed.";
	}
}
