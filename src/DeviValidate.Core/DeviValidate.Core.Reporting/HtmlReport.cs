using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace DeviValidate.Core.Reporting;

public static class HtmlReport
{
	public static string Render(VerificationRecord record)
	{
		string text = ((record.MismatchCount > 0) ? "mismatch" : ((record.MissingCount > 0) ? "missing" : ((record.ExtraCount > 0) ? "review" : "match")));
		string kicker = text switch
		{
			"match" => "Match", 
			"mismatch" => "Does not match", 
			"missing" => "File not found", 
			_ => "Review the file list", 
		};
		StringBuilder stringBuilder = new StringBuilder();
		if (record.MismatchCount > 0)
		{
			stringBuilder.Append("<p class=\"rednote\">").Append(Encode("Red means the new hash is not the same as the hash written down before. The file we read is not the same as the file that hash describes. The notes below say what that can and cannot mean.")).Append("</p>");
		}
		else if (record.MissingCount == 0 && record.ExtraCount == 0)
		{
			stringBuilder.Append("<p class=\"matchnote\">").Append(Encode("A match means the new hash is the same as the hash written down before. The file we read is the same file that hash describes. Anyone can check the file again with sha256sum, certutil -hashfile, or Get-FileHash. This check verifies the bytes only. It does not show who made the file, what the file means, or whether a chain of custody is complete.")).Append("</p>");
		}
		stringBuilder.Append("<p class=\"independent\">").Append(Encode("This record independently recomputes hash values and compares them with values recorded by another tool or process.")).Append("</p>");
		stringBuilder.Append(Fields(RecordFields.Verification(record)));
		stringBuilder.Append("<p class=\"note\">").Append(Encode(RecordFields.FileNameNote(record))).Append("</p>");
		if (!string.IsNullOrWhiteSpace(record.Note))
		{
			stringBuilder.Append("<p class=\"note\">").Append(Encode(record.Note)).Append("</p>");
		}
		stringBuilder.Append(Stats(System.Array.AsReadOnly(new(string, int)[4]
		{
			("Match", record.MatchCount),
			("Mismatch", record.MismatchCount),
			("Missing", record.MissingCount),
			("Extra", record.ExtraCount)
		})));
		stringBuilder.Append("<table class=\"files\"><thead><tr><th>Result</th><th>Path</th><th class=\"size\">Size</th><th>Computed hash</th><th>Expected hash</th></tr></thead><tbody>");
		foreach (FileVerificationEntry file in record.Files)
		{
			long? size = file.Size;
			object obj;
			if (size.HasValue)
			{
				long valueOrDefault = size.GetValueOrDefault();
				obj = ByteSize.Format(valueOrDefault);
			}
			else
			{
				obj = "-";
			}
			string value = (string)obj;
			bool flag = file.Result == "mismatch";
			stringBuilder.Append(flag ? "<tr class=\"bad\"><td>" : "<tr><td>").Append(Encode(ResultLabels.Display(file.Result))).Append("</td><td class=\"path\">")
				.Append(TableWrap.PathHtml(file.Path))
				.Append("</td><td class=\"size\">")
				.Append(Encode(value))
				.Append("</td><td class=\"mono\">")
				.Append(Encode(file.ComputedHash ?? "-"))
				.Append("</td><td class=\"mono\">")
				.Append(Encode(file.ExpectedHash ?? "-"))
				.Append("</td></tr>");
		}
		stringBuilder.Append("</tbody></table>");
		AppendSkipped(stringBuilder, record.Skipped);
		return Document("DEVI Validate verification record", "Verification record", kicker, record.Verdict, text, stringBuilder.ToString(), Explanation(record), record.Integrity.Hash);
	}

	public static string Render(SelfTestRecord record)
	{
		string text = ((record.FailedCount == 0) ? "match" : "mismatch");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<p class=\"independent\">").Append(Encode("This is a tool validation record from the built-in self-test. It is not an evidence verification. The checks hash published test vectors for SHA-256, SHA-1, and MD5, and they confirm that a synthetic file's contents and timestamps were unchanged after hashing. A passing result means this build reproduced the published values and the read-only check on this machine. It does not certify the tool for casework.")).Append("</p>");
		stringBuilder.Append(Fields(RecordFields.SelfTest(record)));
		stringBuilder.Append(Stats(System.Array.AsReadOnly(new(string, int)[2]
		{
			("Passed", record.PassedCount),
			("Failed", record.FailedCount)
		})));
		stringBuilder.Append("<table><thead><tr><th>Check</th><th>Input</th><th>Expected</th><th>Computed</th><th>Result</th></tr></thead><tbody>");
		foreach (SelfTestCheck check in record.Checks)
		{
			stringBuilder.Append(check.Passed ? "<tr><td>" : "<tr class=\"bad\"><td>").Append(Encode(check.Name)).Append("</td><td>")
				.Append(Encode(check.Input))
				.Append("</td><td class=\"mono\">")
				.Append(Encode(check.Expected))
				.Append("</td><td class=\"mono\">")
				.Append(Encode(check.Computed))
				.Append("</td><td>")
				.Append(check.Passed ? "Pass" : "Fail")
				.Append("</td></tr>");
		}
		stringBuilder.Append("</tbody></table>");
		return Document("DEVI Validate tool validation record", "Tool validation record", (text == "match") ? "Pass" : "Fail", record.Result, text, stringBuilder.ToString(), "", record.Integrity.Hash);
	}

	public static string Render(HashManifest manifest)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<dl class=\"grid\">");
		AppendField(stringBuilder, "Source", manifest.SourcePath, mono: false);
		AppendField(stringBuilder, "Algorithm", manifest.Algorithm, mono: false);
		AppendField(stringBuilder, "Computed", TimeDisplay.Format(manifest.CreatedAt, manifest.TimeZone), mono: false);
		AppendField(stringBuilder, "Machine", ReportCopy.OrNotRecorded(manifest.MachineName), mono: false);
		AppendField(stringBuilder, "Examiner", ReportCopy.OrNotRecorded(manifest.Examiner), mono: false);
		AppendField(stringBuilder, "Agency/department", ReportCopy.OrNotRecorded(manifest.Agency), mono: false);
		AppendField(stringBuilder, "Case or reference", ReportCopy.OrNotRecorded(manifest.CaseReference), mono: false);
		stringBuilder.Append("</dl>");
		stringBuilder.Append("<table class=\"files\"><thead><tr><th>Path</th><th class=\"size\">Size</th><th>Hash</th></tr></thead><tbody>");
		foreach (FileHashEntry file in manifest.Files)
		{
			stringBuilder.Append("<tr><td class=\"path\">").Append(TableWrap.PathHtml(file.Path)).Append("</td><td class=\"size\">")
				.Append(Encode(ByteSize.Format(file.Size)))
				.Append("</td><td class=\"mono\">")
				.Append(Encode(file.Hash))
				.Append("</td></tr>");
		}
		stringBuilder.Append("</tbody></table>");
		AppendSkipped(stringBuilder, manifest.Skipped);
		return Document("DEVI Validate hash manifest", "Hash manifest", "Hash complete", manifest.Files.Count.ToString(CultureInfo.InvariantCulture) + " files hashed with " + manifest.Algorithm + ".", "match", stringBuilder.ToString(), "", manifest.Integrity.Hash);
	}

	private static string Explanation(VerificationRecord record)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<section class=\"explanation\"><h2>How to read this record</h2>");
		foreach (ReportSection item in ReportCopy.Explanation)
		{
			stringBuilder.Append("<h3>").Append(Encode(item.Heading)).Append("</h3>");
			string[] array = item.Body.Split("\n\n");
			foreach (string value in array)
			{
				stringBuilder.Append("<p>").Append(Encode(value)).Append("</p>");
			}
			if (item.Heading == "Validation and use")
			{
				stringBuilder.Append("<dl class=\"grid\">");
				AppendField(stringBuilder, "Validated by", ReportCopy.OrNotRecorded(record.ValidatedBy), mono: false);
				AppendField(stringBuilder, "Validation date", ReportCopy.OrNotRecorded(record.ValidationDate), mono: false);
				AppendField(stringBuilder, "Lab procedure/reference", ReportCopy.OrNotRecorded(record.LabProcedure), mono: false);
				stringBuilder.Append("</dl>");
			}
		}
		stringBuilder.Append("</section>");
		return stringBuilder.ToString();
	}

	private static string Fields(IReadOnlyList<RecordFields.Field> fields)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<dl class=\"grid\">");
		foreach (RecordFields.Field field in fields)
		{
			AppendField(stringBuilder, field.Label, field.Value, field.Mono);
		}
		stringBuilder.Append("</dl>");
		return stringBuilder.ToString();
	}

	private static void AppendField(StringBuilder html, string label, string value, bool mono)
	{
		html.Append("<dt>").Append(Encode(label)).Append("</dt><dd");
		if (mono)
		{
			html.Append(" class=\"mono\"");
		}
		html.Append('>').Append(Encode(value)).Append("</dd>");
	}

	private static string Stats(IReadOnlyList<(string Label, int Count)> stats)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<div class=\"stats\">");
		foreach (var stat in stats)
		{
			bool flag = stat.Count > 0;
			if (flag)
			{
				string item = stat.Label;
				bool flag2 = ((item == "Mismatch" || item == "Failed") ? true : false);
				flag = flag2;
			}
			bool flag3 = flag;
			stringBuilder.Append(flag3 ? "<div class=\"stat bad\"><b>" : "<div class=\"stat\"><b>").Append(stat.Count.ToString(CultureInfo.InvariantCulture)).Append("</b><span>")
				.Append(Encode(stat.Label))
				.Append("</span></div>");
		}
		stringBuilder.Append("</div>");
		return stringBuilder.ToString();
	}

	private static string Document(string title, string kind, string kicker, string verdict, string tone, string body, string explanation, string recordHash)
	{
		return "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<meta name=\"generator\" content=\"DEVI Validate __VERSION__\">\n<title>__TITLE__</title>\n<style>\n:root { color-scheme: light; }\n* { box-sizing: border-box; }\nbody { margin: 0; background: #f4f6f8; color: #161616; font: 15px/1.45 \"Liberation Sans\", \"Segoe UI\", sans-serif; }\nmain { max-width: 840px; margin: 28px auto; background: #fff; padding: 36px 40px 28px; }\nheader { display: flex; justify-content: space-between; align-items: center; background: #0A0A0B; color: #fff; margin: -36px -40px 0; padding: 18px 40px; }\nheader .version { color: #A1A1A8; }\nh1.brand { display: flex; align-items: center; gap: 10px; margin: 0; font-size: 22px; font-weight: 400; }\nh1.brand .wordmark svg { height: 22px; width: auto; display: block; }\nh1.brand .name { font-weight: 400; }\n.version, .kind { color: #5c5a55; font-size: 13px; }\n.kind { margin: 14px 0 8px; letter-spacing: 0.08em; text-transform: uppercase; }\n.verdict { padding: 18px 20px; margin: 0 0 10px; }\n.verdict p { margin: 6px 0 0; font-size: 22px; font-weight: 650; line-height: 1.25; }\n.verdict.match { background: #e5f6ec; color: #0e3d28; }\n.verdict.mismatch { background: #9e1c1c; color: #fff; }\n.verdict.missing, .verdict.review { background: #fff; color: #161616; border: 2px solid #4B8DF8; }\n.rednote { color: #9e1c1c; font-weight: 650; margin: 0 0 12px; }\n.matchnote { color: #0e5c3a; font-weight: 650; margin: 0 0 12px; }\n.independent, .note { color: #3f3d38; font-size: 13px; margin: 0 0 14px; }\n.grid { display: grid; grid-template-columns: 180px 1fr; gap: 7px 16px; margin: 0 0 14px; }\n.grid dt { color: #5c5a55; }\n.grid dd { margin: 0; overflow-wrap: anywhere; }\n.mono { font-family: \"Liberation Mono\", Consolas, monospace; font-size: 12px; }\n.stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 8px; margin: 0 0 16px; }\n.stat { background: #eef3f8; padding: 10px 12px; }\n.stat b { display: block; font-size: 22px; font-weight: 650; }\n.stat span { color: #5c5a55; font-size: 12px; }\n.stat.bad { background: #f8e4e4; }\n.stat.bad b, .stat.bad span { color: #9e1c1c; }\ntr.bad td { color: #9e1c1c; font-weight: 650; }\ntable { width: 100%; border-collapse: collapse; }\nth { text-align: left; font-size: 12px; color: #5c5a55; font-weight: 650; border-bottom: 1px solid #161616; padding: 6px 8px 6px 0; }\ntd { vertical-align: top; border-bottom: 1px solid #e4e0d8; padding: 8px 8px 8px 0; overflow-wrap: normal; }\ntd.path { overflow-wrap: break-word; }\nth.size, td.size { text-align: right; white-space: nowrap; width: 1%; }\ntd.mono { overflow-wrap: anywhere; }\nfooter { margin-top: 22px; border-top: 1px solid #e4e0d8; padding-top: 10px; color: #5c5a55; font-size: 12px; }\nfooter .mono { color: #161616; }\n.explanation { margin-top: 28px; border-top: 1px solid #161616; padding-top: 8px; }\n.explanation h2 { font-size: 18px; margin: 12px 0; }\n.explanation h3 { font-size: 14px; margin: 16px 0 4px; }\n.explanation p { margin: 0 0 8px; }\n@media print {\n  body { background: #fff; }\n  header { margin: 0 0 0; print-color-adjust: exact; -webkit-print-color-adjust: exact; }\n  main { margin: 0; max-width: none; padding: 0; }\n  .explanation { break-before: page; }\n  .verdict, .stat { print-color-adjust: exact; -webkit-print-color-adjust: exact; }\n}\n@media (max-width: 720px) {\n  main { margin: 0; padding: 20px; }\n  header { margin: -20px -20px 0; padding: 16px 20px; }\n  .grid, .stats { grid-template-columns: 1fr 1fr; }\n}\n</style>\n</head>\n<body>\n<main>\n<header>\n  <h1 class=\"brand\"><span class=\"wordmark\">__WORDMARK__</span><span class=\"name\">Validate</span></h1>\n  <div class=\"version\">__VERSION__</div>\n</header>\n<div class=\"kind\">__KIND__</div>\n<section class=\"verdict __TONE__\">\n  <div>__KICKER__</div>\n  <p>__VERDICT__</p>\n</section>\n__BODY__\n<footer>\n  <div>Record SHA-256</div>\n  <div class=\"mono\">__HASH__</div>\n  <div>This hash covers the canonical JSON record. It does not cover this HTML page or the PDF.</div>\n</footer>\n__EXPLANATION__\n</main>\n</body>\n</html>".Replace("__WORDMARK__", BrandAssets.WordmarkWhiteSvg, StringComparison.Ordinal).Replace("__VERSION__", Encode(ToolInfo.Version), StringComparison.Ordinal).Replace("__TITLE__", Encode(title), StringComparison.Ordinal)
			.Replace("__KIND__", Encode(kind), StringComparison.Ordinal)
			.Replace("__KICKER__", Encode(kicker), StringComparison.Ordinal)
			.Replace("__VERDICT__", Encode(verdict), StringComparison.Ordinal)
			.Replace("__TONE__", tone, StringComparison.Ordinal)
			.Replace("__BODY__", body, StringComparison.Ordinal)
			.Replace("__HASH__", Encode(recordHash), StringComparison.Ordinal)
			.Replace("__EXPLANATION__", explanation, StringComparison.Ordinal);
	}

	private static void AppendSkipped(StringBuilder body, IReadOnlyList<SkippedEntry> skipped)
	{
		if (skipped.Count == 0)
		{
			return;
		}
		body.Append("<p class=\"note\">").Append(Encode(ReportCopy.SkippedSummary(skipped.Count))).Append("</p><ul>");
		foreach (SkippedEntry item in skipped)
		{
			body.Append("<li><span class=\"mono\">").Append(Encode(item.Path)).Append("</span>: ")
				.Append(Encode(ReportCopy.SkippedDetail(item)))
				.Append("</li>");
		}
		body.Append("</ul>");
	}

	private static string Encode(string? value)
	{
		return WebUtility.HtmlEncode(value ?? "");
	}
}
