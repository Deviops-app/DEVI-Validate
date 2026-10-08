using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DeviValidate.Core;
using DeviValidate.Core.Expected;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.Reporting;
using DeviValidate.Core.SelfTest;
using Devi.Updates;

namespace DeviValidate.Cli;

internal static class CliApp
{
	private const string HelpText = "DEVI Validate 1.0.3\nIndependently recompute hash values and compare them with values recorded by another tool or process.\n\nUsage:\n  devi-validate hash <path> [options]\n  devi-validate verify <path> (--expected <file> | --hash <hex>) [options]\n  devi-validate selftest [--output-dir <folder>]\n  devi-validate report <record.json> (--output <file.html> | --output-dir <folder>)\n  devi-validate check <record.json>\n  devi-validate verify-package <folder|package.zip> [--code <DV1-code>]\n  devi-validate update [--download <folder>] [--portable]\n  devi-validate --help\n  devi-validate --version\n\nhash\n  Compute hashes for a file or a folder.\n  --algorithm, -a   sha256 (default), sha1, or md5\n  --output, -o      Manifest JSON to write. The folder must be outside the evidence path.\n  --examiner        Examiner name stored in the manifest.\n  --case            Case or reference number stored in the manifest.\n  --quiet           Do not print read progress.\n\nverify\n  Hash the evidence and compare it with expected hashes.\n  --expected, -e    Sum file, CSV, TSV, FTK Imager text log, or DEVI Validate JSON.\n  --hash            Expected hash for a single file.\n  --algorithm, -a   sha256, sha1, or md5. Inferred from the expected hashes when omitted.\n  --output-dir, -d  Folder for the HTML, JSON, and PDF records. Must be outside the evidence path.\n                    A single file is named with its computed hash. A folder is named with the set hash.\n  --pdf             Write the PDF to this path as well.\n  --examiner        Examiner name stored in the record.\n  --case            Case or reference number stored in the record.\n  --validated-by    Optional name printed on the record. Blank prints \"Not recorded\".\n  --validation-date Optional date printed on the record. Blank prints \"Not recorded\".\n  --lab-procedure   Optional lab procedure or reference. Blank prints \"Not recorded\".\n  --path-column     CSV or TSV path column, by header name or 1-based index.\n  --hash-column     CSV or TSV hash column, by header name or 1-based index.\n  --format          auto (default), sum, csv, tsv, manifest, or ftk.\n  --ignore-path-case\n                    Compare paths without regard to letter case.\n  --quiet           Do not print read progress.\n\nselftest\n  Hash published SHA-256, SHA-1, and MD5 test vectors, and check that a synthetic file is unchanged.\n  Writes a tool validation record named selftest_<version>_<date>.pdf, plus HTML and JSON.\n  --output-dir, -d  Folder for the record. The current folder is used when this is omitted.\n  --validated-by    Optional name printed on the record.\n  --validation-date Optional date printed on the record.\n  --lab-procedure   Optional lab procedure or reference.\n\nreport\n  Write HTML and PDF from a DEVI Validate JSON file.\n  --output, -o      HTML file to write.\n  --output-dir, -d  Folder for the records. Verification files are named with the hash.\n  --pdf             PDF file to write.\n  --evidence        Evidence path used only to refuse a report written inside it.\n\ncheck\n  Recompute the record SHA-256 and compare it with the stored integrity hash.\n\nverify-package\n  Re-verify a validation package offline: the SHA-256 of every file in package-manifest.json,\n  the record hash, the verification code, and the signature when the package is signed.\n  --code            The code printed on the PDF (DV1-...), the full record SHA-256, or the QR code text.\n\nupdate\n  Fetch the signed version file and compare it with this copy.\n  Does not download or install unless --download is set. Hash and verify do not call this.\n  Exit 0 when this copy is current or newer than the published version.\n  Exit 2 when a newer version is published and --download was not set.\n  --download <folder>  Download the installer, or the zip with --portable, check SHA-256, and stop.\n  --portable           With --download, fetch the zip instead of the installer.\n\nsign-update <feed.json> --key <private.pem>\n  Sign a version file with the offline private key. Does not use the network.\n  --output          Write the signed file here. Otherwise the input path is replaced.\n\nupdate-key --out <private.pem>\n  Write a new ECDSA P-256 private key and print the public key.\n  This does not change the key built into this copy.\n\nExit codes:\n  0   Hash finished, every file matched, the record hash matched, every self-test check passed, or no newer version is published.\n  1   The command could not run.\n  2   A mismatch, missing path, extra file, integrity difference, failed self-test check, or available update was reported.\n  130 Canceled.\n\nEvidence is opened read-only. Reports are not written inside the evidence location.\nFor a single file, that location is the folder that contains the file.\nIf an export name is already in use, the tool writes name_2, name_3, and so on.\nHashing and verification do not use the network. update is the only command that contacts the download host, and only when you run it.\n\nProject page: https://deviops.app";

	public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
	{
		bool flag = args.Length == 0;
		bool flag2;
		if (!flag)
		{
			if (args == null || args.Length != 1)
			{
				goto IL_0079;
			}
			switch (args[0])
			{
			case "--help":
			case "-h":
			case "help":
				break;
			default:
				goto IL_0079;
			}
			flag2 = true;
			goto IL_007c;
		}
		goto IL_0080;
		IL_0079:
		flag2 = false;
		goto IL_007c;
		IL_007c:
		flag = flag2;
		goto IL_0080;
		IL_0080:
		if (flag)
		{
			Console.WriteLine("DEVI Validate 1.0.3\nIndependently recompute hash values and compare them with values recorded by another tool or process.\n\nUsage:\n  devi-validate hash <path> [options]\n  devi-validate verify <path> (--expected <file> | --hash <hex>) [options]\n  devi-validate selftest [--output-dir <folder>]\n  devi-validate report <record.json> (--output <file.html> | --output-dir <folder>)\n  devi-validate check <record.json>\n  devi-validate verify-package <folder|package.zip> [--code <DV1-code>]\n  devi-validate update [--download <folder>] [--portable]\n  devi-validate --help\n  devi-validate --version\n\nhash\n  Compute hashes for a file or a folder.\n  --algorithm, -a   sha256 (default), sha1, or md5\n  --output, -o      Manifest JSON to write. The folder must be outside the evidence path.\n  --examiner        Examiner name stored in the manifest.\n  --case            Case or reference number stored in the manifest.\n  --quiet           Do not print read progress.\n\nverify\n  Hash the evidence and compare it with expected hashes.\n  --expected, -e    Sum file, CSV, TSV, FTK Imager text log, or DEVI Validate JSON.\n  --hash            Expected hash for a single file.\n  --algorithm, -a   sha256, sha1, or md5. Inferred from the expected hashes when omitted.\n  --output-dir, -d  Folder for the HTML, JSON, and PDF records. Must be outside the evidence path.\n                    A single file is named with its computed hash. A folder is named with the set hash.\n  --pdf             Write the PDF to this path as well.\n  --examiner        Examiner name stored in the record.\n  --case            Case or reference number stored in the record.\n  --validated-by    Optional name printed on the record. Blank prints \"Not recorded\".\n  --validation-date Optional date printed on the record. Blank prints \"Not recorded\".\n  --lab-procedure   Optional lab procedure or reference. Blank prints \"Not recorded\".\n  --path-column     CSV or TSV path column, by header name or 1-based index.\n  --hash-column     CSV or TSV hash column, by header name or 1-based index.\n  --format          auto (default), sum, csv, tsv, manifest, or ftk.\n  --ignore-path-case\n                    Compare paths without regard to letter case.\n  --quiet           Do not print read progress.\n\nselftest\n  Hash published SHA-256, SHA-1, and MD5 test vectors, and check that a synthetic file is unchanged.\n  Writes a tool validation record named selftest_<version>_<date>.pdf, plus HTML and JSON.\n  --output-dir, -d  Folder for the record. The current folder is used when this is omitted.\n  --validated-by    Optional name printed on the record.\n  --validation-date Optional date printed on the record.\n  --lab-procedure   Optional lab procedure or reference.\n\nreport\n  Write HTML and PDF from a DEVI Validate JSON file.\n  --output, -o      HTML file to write.\n  --output-dir, -d  Folder for the records. Verification files are named with the hash.\n  --pdf             PDF file to write.\n  --evidence        Evidence path used only to refuse a report written inside it.\n\ncheck\n  Recompute the record SHA-256 and compare it with the stored integrity hash.\n\nverify-package\n  Re-verify a validation package offline: the SHA-256 of every file in package-manifest.json,\n  the record hash, the verification code, and the signature when the package is signed.\n  --code            The code printed on the PDF (DV1-...), the full record SHA-256, or the QR code text.\n\nupdate\n  Fetch the signed version file and compare it with this copy.\n  Does not download or install unless --download is set. Hash and verify do not call this.\n  Exit 0 when this copy is current or newer than the published version.\n  Exit 2 when a newer version is published and --download was not set.\n  --download <folder>  Download the installer, or the zip with --portable, check SHA-256, and stop.\n  --portable           With --download, fetch the zip instead of the installer.\n\nsign-update <feed.json> --key <private.pem>\n  Sign a version file with the offline private key. Does not use the network.\n  --output          Write the signed file here. Otherwise the input path is replaced.\n\nupdate-key --out <private.pem>\n  Write a new ECDSA P-256 private key and print the public key.\n  This does not change the key built into this copy.\n\nExit codes:\n  0   Hash finished, every file matched, the record hash matched, every self-test check passed, or no newer version is published.\n  1   The command could not run.\n  2   A mismatch, missing path, extra file, integrity difference, failed self-test check, or available update was reported.\n  130 Canceled.\n\nEvidence is opened read-only. Reports are not written inside the evidence location.\nFor a single file, that location is the folder that contains the file.\nIf an export name is already in use, the tool writes name_2, name_3, and so on.\nHashing and verification do not use the network. update is the only command that contacts the download host, and only when you run it.\n\nProject page: https://deviops.app");
			return 0;
		}
		if (args != null && args.Length == 1)
		{
			string text = args[0];
			if (text == "--version" || text == "version")
			{
				flag = true;
				goto IL_00d6;
			}
		}
		flag = false;
		goto IL_00d6;
		IL_00d6:
		if (flag)
		{
			Console.WriteLine("DEVI Validate 1.0.3");
			return 0;
		}
		string text2 = args[0];
		ArgList argList = new ArgList(args.Skip(1));
		if (argList.Has("help"))
		{
			Console.WriteLine("DEVI Validate 1.0.3\nIndependently recompute hash values and compare them with values recorded by another tool or process.\n\nUsage:\n  devi-validate hash <path> [options]\n  devi-validate verify <path> (--expected <file> | --hash <hex>) [options]\n  devi-validate selftest [--output-dir <folder>]\n  devi-validate report <record.json> (--output <file.html> | --output-dir <folder>)\n  devi-validate check <record.json>\n  devi-validate verify-package <folder|package.zip> [--code <DV1-code>]\n  devi-validate update [--download <folder>] [--portable]\n  devi-validate --help\n  devi-validate --version\n\nhash\n  Compute hashes for a file or a folder.\n  --algorithm, -a   sha256 (default), sha1, or md5\n  --output, -o      Manifest JSON to write. The folder must be outside the evidence path.\n  --examiner        Examiner name stored in the manifest.\n  --case            Case or reference number stored in the manifest.\n  --quiet           Do not print read progress.\n\nverify\n  Hash the evidence and compare it with expected hashes.\n  --expected, -e    Sum file, CSV, TSV, FTK Imager text log, or DEVI Validate JSON.\n  --hash            Expected hash for a single file.\n  --algorithm, -a   sha256, sha1, or md5. Inferred from the expected hashes when omitted.\n  --output-dir, -d  Folder for the HTML, JSON, and PDF records. Must be outside the evidence path.\n                    A single file is named with its computed hash. A folder is named with the set hash.\n  --pdf             Write the PDF to this path as well.\n  --examiner        Examiner name stored in the record.\n  --case            Case or reference number stored in the record.\n  --validated-by    Optional name printed on the record. Blank prints \"Not recorded\".\n  --validation-date Optional date printed on the record. Blank prints \"Not recorded\".\n  --lab-procedure   Optional lab procedure or reference. Blank prints \"Not recorded\".\n  --path-column     CSV or TSV path column, by header name or 1-based index.\n  --hash-column     CSV or TSV hash column, by header name or 1-based index.\n  --format          auto (default), sum, csv, tsv, manifest, or ftk.\n  --ignore-path-case\n                    Compare paths without regard to letter case.\n  --quiet           Do not print read progress.\n\nselftest\n  Hash published SHA-256, SHA-1, and MD5 test vectors, and check that a synthetic file is unchanged.\n  Writes a tool validation record named selftest_<version>_<date>.pdf, plus HTML and JSON.\n  --output-dir, -d  Folder for the record. The current folder is used when this is omitted.\n  --validated-by    Optional name printed on the record.\n  --validation-date Optional date printed on the record.\n  --lab-procedure   Optional lab procedure or reference.\n\nreport\n  Write HTML and PDF from a DEVI Validate JSON file.\n  --output, -o      HTML file to write.\n  --output-dir, -d  Folder for the records. Verification files are named with the hash.\n  --pdf             PDF file to write.\n  --evidence        Evidence path used only to refuse a report written inside it.\n\ncheck\n  Recompute the record SHA-256 and compare it with the stored integrity hash.\n\nverify-package\n  Re-verify a validation package offline: the SHA-256 of every file in package-manifest.json,\n  the record hash, the verification code, and the signature when the package is signed.\n  --code            The code printed on the PDF (DV1-...), the full record SHA-256, or the QR code text.\n\nupdate\n  Fetch the signed version file and compare it with this copy.\n  Does not download or install unless --download is set. Hash and verify do not call this.\n  Exit 0 when this copy is current or newer than the published version.\n  Exit 2 when a newer version is published and --download was not set.\n  --download <folder>  Download the installer, or the zip with --portable, check SHA-256, and stop.\n  --portable           With --download, fetch the zip instead of the installer.\n\nsign-update <feed.json> --key <private.pem>\n  Sign a version file with the offline private key. Does not use the network.\n  --output          Write the signed file here. Otherwise the input path is replaced.\n\nupdate-key --out <private.pem>\n  Write a new ECDSA P-256 private key and print the public key.\n  This does not change the key built into this copy.\n\nExit codes:\n  0   Hash finished, every file matched, the record hash matched, every self-test check passed, or no newer version is published.\n  1   The command could not run.\n  2   A mismatch, missing path, extra file, integrity difference, failed self-test check, or available update was reported.\n  130 Canceled.\n\nEvidence is opened read-only. Reports are not written inside the evidence location.\nFor a single file, that location is the folder that contains the file.\nIf an export name is already in use, the tool writes name_2, name_3, and so on.\nHashing and verification do not use the network. update is the only command that contacts the download host, and only when you run it.\n\nProject page: https://deviops.app");
			return 0;
		}
		return text2 switch
		{
			"hash" => await HashAsync(argList, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), 
			"verify" => await VerifyAsync(argList, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), 
			"report" => Report(argList), 
			"check" => Check(argList), 
			"verify-package" => VerifyPackage(argList), 
			"selftest" => await SelfTestAsync(argList, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), 
			"update" => await UpdateAsync(argList, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), 
			"sign-update" => SignUpdate(argList), 
			"update-key" => UpdateKey(argList), 
			_ => throw new CliException("Unknown command '" + text2 + "'. Run 'devi-validate --help'."), 
		};
	}

	private static int VerifyPackage(ArgList args)
	{
		args.RequireKnown("code", "help");
		if (args.Positionals.Count != 1)
		{
			throw new CliException("Usage: devi-validate verify-package <folder|package.zip|package-manifest.json> [--code <DV1-code>]");
		}
		DeviValidate.Core.Packaging.PackageVerification result = DeviValidate.Core.Packaging.PackageVerifier.Verify(args.Positionals[0], args.Get("code"));
		foreach (DeviValidate.Core.Packaging.PackageCheck check in result.Checks)
		{
			string label = check.Outcome switch
			{
				DeviValidate.Core.Packaging.CheckOutcome.Pass => "PASS  ",
				DeviValidate.Core.Packaging.CheckOutcome.Fail => "FAIL  ",
				DeviValidate.Core.Packaging.CheckOutcome.Warning => "REVIEW",
				_ => "NOTE  "
			};
			Console.WriteLine(label + "  " + check.Name + ": " + check.Detail);
		}
		Console.WriteLine();
		Console.WriteLine(result.Summary);
		return result.Passed ? 0 : 2;
	}

	private static async Task<int> HashAsync(ArgList args, CancellationToken cancellationToken)
	{
		args.RequireKnown("algorithm", "output", "examiner", "case", "quiet", "help");
		if (args.Positionals.Count != 1)
		{
			throw new CliException("Usage: devi-validate hash <path> [--algorithm sha256|sha1|md5] [--output <manifest.json>]");
		}
		string evidence = args.Positionals[0];
		HashAlgorithmKind valueOrDefault = ReadAlgorithm(args.Get("algorithm"), required: false).GetValueOrDefault();
		HashProgressWriter progress = new HashProgressWriter(args.Has("quiet"));
		HashManifest hashManifest = await VerificationWorkflow.HashAsync(evidence, valueOrDefault, args.Get("examiner"), args.Get("case"), progress, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		progress.Finish();
		string writtenPath = null;
		string text = args.Get("output");
		if (text != null)
		{
			writtenPath = RecordStore.WriteManifest(evidence, text, hashManifest);
		}
		ConsoleReport.WriteManifest(hashManifest, writtenPath);
		return (hashManifest.Skipped.Count > 0) ? 2 : 0;
	}

	private static async Task<int> VerifyAsync(ArgList args, CancellationToken cancellationToken)
	{
		args.RequireKnown("expected", "hash", "algorithm", "output-dir", "pdf", "examiner", "case", "validated-by", "validation-date", "lab-procedure", "path-column", "hash-column", "format", "ignore-path-case", "quiet", "help");
		if (args.Positionals.Count != 1)
		{
			throw new CliException("Usage: devi-validate verify <path> (--expected <file> | --hash <hex>) [--output-dir <folder>]");
		}
		bool num = args.Get("expected") != null;
		bool flag = args.Get("hash") != null;
		if (num == flag)
		{
			throw new CliException("Pass either --expected or --hash.");
		}
		string evidence = args.Positionals[0];
		HashAlgorithmKind? algorithm = ReadAlgorithm(args.Get("algorithm"), required: false);
		ExpectedHashes expected;
		string expectedSource;
		if (flag)
		{
			expected = VerificationWorkflow.SingleHash(evidence, args.Get("hash"), algorithm);
			expectedSource = "pasted hash";
		}
		else
		{
			expectedSource = Path.GetFullPath(args.Get("expected"));
			expected = VerificationWorkflow.ReadExpected(expectedSource, new ExpectedReadOptions
			{
				Algorithm = algorithm,
				PathColumn = args.Get("path-column"),
				HashColumn = args.Get("hash-column"),
				Format = args.Get("format")
			});
		}
		ExpectedHashRules.EnsureCompatible(evidence, expected);
		HashProgressWriter progress = new HashProgressWriter(args.Has("quiet"));
		HashManifest computed = await VerificationWorkflow.HashAsync(evidence, expected.Algorithm, args.Get("examiner"), args.Get("case"), progress, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		progress.Finish();
		VerificationRecord verificationRecord = VerificationWorkflow.Verify(computed, expected, evidence, expectedSource, args.Get("examiner"), args.Get("case"), args.Has("ignore-path-case"), args.Get("validated-by"), args.Get("validation-date"), args.Get("lab-procedure"));
		WrittenReports writtenReports = null;
		string text = args.Get("output-dir");
		if (text != null)
		{
			writtenReports = RecordStore.WriteVerification(evidence, text, verificationRecord);
		}
		string text2 = null;
		string text3 = args.Get("pdf");
		if (text3 != null)
		{
			text2 = RecordStore.WriteVerificationPdf(evidence, text3, verificationRecord);
			if ((object)writtenReports == null)
			{
				writtenReports = new WrittenReports(null, null, text2);
			}
		}
		ConsoleReport.WriteVerification(verificationRecord, writtenReports);
		if (text2 != null && !string.Equals(text2, writtenReports?.PdfPath, StringComparison.Ordinal))
		{
			Console.WriteLine($"{"PDF copy",-12} {text2}");
		}
		return (verificationRecord.MismatchCount > 0 || verificationRecord.MissingCount > 0 || verificationRecord.ExtraCount > 0 || verificationRecord.Skipped.Count > 0) ? 2 : 0;
	}

	private static async Task<int> SelfTestAsync(ArgList args, CancellationToken cancellationToken)
	{
		args.RequireKnown("output-dir", "validated-by", "validation-date", "lab-procedure", "help");
		if (args.Positionals.Count != 0)
		{
			throw new CliException("Usage: devi-validate selftest [--output-dir <folder>]");
		}
		string directory = args.Get("output-dir") ?? Directory.GetCurrentDirectory();
		SelfTestRecord selfTestRecord = await SelfTestRunner.RunAsync(ExaminationContext.Capture(null, null, args.Get("validated-by"), args.Get("validation-date"), args.Get("lab-procedure")), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		WrittenReports written = RecordStore.WriteSelfTest(null, directory, selfTestRecord);
		ConsoleReport.WriteSelfTest(selfTestRecord, written);
		return (selfTestRecord.FailedCount != 0) ? 2 : 0;
	}

	private static int Report(ArgList args)
	{
		args.RequireKnown("output", "output-dir", "pdf", "evidence", "help");
		if (args.Positionals.Count != 1)
		{
			throw new CliException("Usage: devi-validate report <record.json> (--output <file.html> | --output-dir <folder>)");
		}
		string evidencePath;
		IStampedRecord stampedRecord = RecordStore.ReadStamped(args.Positionals[0], out evidencePath);
		string text = args.Get("evidence") ?? evidencePath;
		if (!(stampedRecord is SelfTestRecord) && string.IsNullOrWhiteSpace(text))
		{
			throw new CliException("The JSON record has no evidence path. Pass --evidence so the report can be kept outside it.");
		}
		if (args.Has("output") && args.Has("output-dir"))
		{
			throw new CliException("Pass only one of --output and --output-dir.");
		}
		string text2 = args.Get("output-dir");
		if (text2 != null && stampedRecord is VerificationRecord record)
		{
			WrittenReports writtenReports = RecordStore.WriteVerification(text, text2, record);
			Console.WriteLine("HTML     " + writtenReports.HtmlPath);
			Console.WriteLine("JSON     " + writtenReports.JsonPath);
			Console.WriteLine("PDF      " + writtenReports.PdfPath);
			string text3 = args.Get("pdf");
			if (text3 != null)
			{
				Console.WriteLine("PDF copy " + RecordStore.WriteVerificationPdf(text, text3, record));
			}
			return 0;
		}
		string text4 = args.Get("output-dir");
		if (text4 != null && stampedRecord is SelfTestRecord record2)
		{
			WrittenReports writtenReports2 = RecordStore.WriteSelfTest(string.IsNullOrWhiteSpace(text) ? null : text, text4, record2);
			Console.WriteLine("PDF      " + writtenReports2.PdfPath);
			Console.WriteLine("HTML     " + writtenReports2.HtmlPath);
			Console.WriteLine("JSON     " + writtenReports2.JsonPath);
			return 0;
		}
		string text5 = args.Get("output");
		if (text5 == null && args.Get("pdf") == null)
		{
			text5 = Path.Combine(args.Get("output-dir") ?? throw new CliException("Pass --output, --output-dir, or --pdf."), (stampedRecord is HashManifest) ? "DEVI-Validate-manifest.html" : "DEVI-Validate-verification.html");
		}
		if (text5 != null)
		{
			string text6;
			if (!(stampedRecord is VerificationRecord record3))
			{
				if (!(stampedRecord is HashManifest manifest))
				{
					if (!(stampedRecord is SelfTestRecord record4))
					{
						throw new InvalidDataException("Unsupported record type.");
					}
					text6 = RecordStore.WriteSelfTest(string.IsNullOrWhiteSpace(text) ? null : text, Path.GetDirectoryName(Path.GetFullPath(text5)) ?? ".", record4).HtmlPath;
				}
				else
				{
					text6 = RecordStore.WriteManifestHtml(text, text5, manifest);
				}
			}
			else
			{
				text6 = RecordStore.WriteVerificationHtml(text, text5, record3);
			}
			string text7 = text6;
			Console.WriteLine("HTML     " + text7);
		}
		string text8 = args.Get("pdf");
		if (text8 != null)
		{
			string text6;
			if (!(stampedRecord is VerificationRecord record5))
			{
				if (!(stampedRecord is SelfTestRecord record6))
				{
					throw new CliException("PDF output is available for a verification record or a tool validation record.");
				}
				text6 = RecordStore.WriteSelfTest(string.IsNullOrWhiteSpace(text) ? null : text, Path.GetDirectoryName(Path.GetFullPath(text8)) ?? ".", record6).PdfPath;
			}
			else
			{
				text6 = RecordStore.WriteVerificationPdf(text, text8, record5);
			}
			string text9 = text6;
			Console.WriteLine("PDF      " + text9);
		}
		return 0;
	}

	private static int Check(ArgList args)
	{
		args.RequireKnown("help");
		if (args.Positionals.Count != 1)
		{
			throw new CliException("Usage: devi-validate check <record.json>");
		}
		string evidencePath;
		IStampedRecord stampedRecord = RecordStore.ReadStamped(args.Positionals[0], out evidencePath);
		string hash = stampedRecord.Integrity.Hash;
		if (RecordIntegrity.Matches(stampedRecord))
		{
			Console.WriteLine("Record SHA-256 matches the stored integrity hash.");
			Console.WriteLine(hash);
			return 0;
		}
		Console.WriteLine("Record SHA-256 does not match the stored integrity hash.");
		Console.WriteLine("stored    " + hash);
		return 2;
	}

	private static HashAlgorithmKind? ReadAlgorithm(string? text, bool required)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			if (required)
			{
				throw new CliException("Pass --algorithm sha256, sha1, or md5.");
			}
			return null;
		}
		if (!HashAlgorithms.TryParse(text, out var kind))
		{
			throw new CliException("Algorithm must be sha256, sha1, or md5.");
		}
		return kind;
	}

	private static async Task<int> UpdateAsync(ArgList args, CancellationToken cancellationToken)
	{
		args.RequireKnown("download", "portable", "help");
		if (args.Positionals.Count != 0)
		{
			throw new CliException("Usage: devi-validate update [--download <folder>] [--portable]");
		}
		using HttpClient http = UpdateClient.CreateFeedClient("DEVI-Validate", ToolInfo.Version);
		using ECDsa key = UpdateTrust.CreatePublicKey();
		UpdateCheckResult updateCheckResult = await UpdateClient.CheckConfiguredAsync(http, "devi-validate", Version.Parse(ToolInfo.Version), key, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		Console.WriteLine(updateCheckResult.Message);
		if (updateCheckResult.Status == UpdateStatus.UpdateAvailable)
		{
			UpdateProduct? product = updateCheckResult.Product;
			if (product != null && product.Notes.Length > 0)
			{
				Console.WriteLine(updateCheckResult.Product.Notes);
			}
		}
		string text = args.Get("download");
		if (text == null)
		{
			return (updateCheckResult.Status == UpdateStatus.UpdateAvailable) ? 2 : 0;
		}
		if (updateCheckResult.Product == null)
		{
			throw new UpdateException("The update feed has no release to download.");
		}
		UpdateFile updateFile = (args.Has("portable") ? (updateCheckResult.Product.Portable ?? throw new UpdateException("The published release has no portable zip.")) : (updateCheckResult.Product.PreferredFile ?? throw new UpdateException("The published release has no file to download.")));
		Console.WriteLine("Downloading " + updateFile.Name);
		string value = await UpdateClient.DownloadVerifiedAsync(http, updateFile, text, null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		Console.WriteLine("SHA-256 matches.");
		Console.WriteLine(value);
		Console.WriteLine("The file was not started. Windows SmartScreen may warn because this build is not Authenticode-signed.");
		return 0;
	}

	private static int SignUpdate(ArgList args)
	{
		args.RequireKnown("key", "output", "help");
		if (args.Positionals.Count != 1)
		{
			throw new CliException("Usage: devi-validate sign-update <feed.json> --key <private.pem>");
		}
		string path = args.Get("key") ?? throw new CliException("Pass --key <private.pem>.");
		string text = args.Positionals[0];
		IReadOnlyList<UpdateProduct> products = UpdateFeed.ParseUnsigned(File.ReadAllText(text));
		using ECDsa eCDsa = ECDsa.Create();
		eCDsa.ImportFromPem(File.ReadAllText(path));
		string contents = UpdateFeed.ToSignedDocument(products, UpdateFeed.Sign(products, eCDsa));
		string text2 = args.Get("output") ?? text;
		File.WriteAllText(text2, contents);
		Console.WriteLine("Signed " + text2);
		return 0;
	}

	private static int UpdateKey(ArgList args)
	{
		args.RequireKnown("out", "help");
		string text = args.Get("out") ?? throw new CliException("Pass --out <private.pem>.");
		using ECDsa eCDsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
		string contents = eCDsa.ExportPkcs8PrivateKeyPem();
		File.WriteAllText(text, contents);
		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(text, UnixFileMode.UserWrite | UnixFileMode.UserRead);
		}
		Console.WriteLine("Wrote " + text);
		Console.WriteLine("Public key (SubjectPublicKeyInfo, base64):");
		Console.WriteLine(Convert.ToBase64String(eCDsa.ExportSubjectPublicKeyInfo()));
		Console.WriteLine("This does not change the key built into DEVI Validate. A new public key needs a new release.");
		return 0;
	}
}
