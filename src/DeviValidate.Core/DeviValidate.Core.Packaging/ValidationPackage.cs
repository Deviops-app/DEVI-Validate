using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DeviValidate.Core.IO;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Packaging;

public sealed record PackageResult(string Folder, string ZipPath, string VerificationCode, bool Signed);

/// <summary>
/// A court-ready validation package: the verification record (JSON), the PDF record with a package
/// page (verification code + QR), a README, and package-manifest.json listing the SHA-256 of each
/// file. With an examiner certificate the PDF carries an embedded signature and the manifest gets a
/// detached CMS signature (package-manifest.json.p7s). Without one the package is hash-sealed only,
/// and the manifest and PDF say so.
/// </summary>
public static class ValidationPackage
{
	public const string RecordFile = "verification-record.json";
	public const string PdfFile = "verification-report.pdf";
	public const string ReadmeFile = "README.txt";
	public const string ManifestFile = "package-manifest.json";
	public const string SignatureFile = "package-manifest.json.p7s";

	public static string HowToVerifyText { get; } =
		"Open DEVI Validate, choose Verify a package, and select this folder or its .zip. Validate recomputes the SHA-256 of every file listed in package-manifest.json, recomputes the record hash from verification-record.json, checks that the verification code printed on the PDF and in the QR code comes from that record hash, and checks the signature when the package is signed. Without the tool: run sha256sum (or certutil -hashfile <file> SHA256, or Get-FileHash) on each file and compare with package-manifest.json. The verification code is DV1- followed by the first 20 hexadecimal characters of recordSha256, in groups of four.";

	public static PackageResult Create(string? evidencePath, string outputDirectory, VerificationRecord record, X509Certificate2? signer)
	{
		if (record.Integrity == null || string.IsNullOrWhiteSpace(record.Integrity.Hash))
		{
			RecordIntegrity.Stamp(record);
		}
		RecordIntegrity.EnsureMatches(record, "verification record");
		if (signer != null && !signer.HasPrivateKey)
		{
			throw new InvalidOperationException("The selected certificate has no private key on this computer, so it cannot sign.");
		}
		string recordHash = record.Integrity!.Hash.ToLowerInvariant();
		string code = VerificationCode.FromRecordHash(recordHash);
		DateTimeOffset now = DateTimeOffset.Now;
		string exeHash = BuildIdentity.TryExecutableHash(out string? exeName) ?? "";
		string? coreHash = BuildIdentity.TryCoreHash();
		var stamp = new PackageStamp(code, recordHash, VerificationCode.QrPayload(recordHash), now, TimeZoneInfo.Local.Id,
			exeName, string.IsNullOrEmpty(exeHash) ? null : exeHash, coreHash, signer?.Subject, signer?.Thumbprint);

		Directory.CreateDirectory(outputDirectory);
		string stem = ExportNames.Stem(record) + "-package";
		string folder = UniqueFolder(outputDirectory, stem);
		if (!string.IsNullOrWhiteSpace(evidencePath))
		{
			OutputPathGuard.EnsureOutside(evidencePath, folder);
		}
		Directory.CreateDirectory(folder);

		string recordJson = CanonicalJson.ToPretty(record) + Environment.NewLine;
		TextFiles.WriteOutside(evidencePath, Path.Combine(folder, RecordFile), recordJson);
		byte[] pdf = PdfReport.Render(record, stamp, signer);
		TextFiles.WriteBytesOutside(evidencePath, Path.Combine(folder, PdfFile), pdf);
		TextFiles.WriteOutside(evidencePath, Path.Combine(folder, ReadmeFile), Readme(record, stamp));

		var manifest = new PackageManifest
		{
			Executable = exeName,
			ExecutableSha256 = stamp.ExecutableSha256,
			CoreSha256 = coreHash,
			CreatedAtUtc = Utc(now),
			CreatedAtLocal = Local(now),
			TimeZone = TimeZoneInfo.Local.Id,
			MachineName = Environment.MachineName,
			Examiner = record.Examiner,
			Agency = record.Agency,
			CaseReference = record.CaseReference,
			ValidatedBy = record.ValidatedBy,
			ValidationDate = record.ValidationDate,
			LabProcedure = record.LabProcedure,
			VerifiedAtUtc = Utc(record.VerifiedAt),
			VerifiedAtLocal = Local(record.VerifiedAt),
			Verdict = record.Verdict,
			Algorithm = record.Algorithm,
			EvidencePath = record.EvidencePath,
			EvidenceKind = record.EvidenceKind,
			EvidenceHash = record.SetHash ?? record.Files.Select(f => f.ComputedHash).FirstOrDefault(h => !string.IsNullOrWhiteSpace(h)),
			RecordSha256 = recordHash,
			VerificationCode = code,
			QrPayload = stamp.QrPayload,
			HowToVerify = HowToVerifyText,
			Signature = signer == null
				? new PackageSignature { Signed = false, Method = "none (hash-sealed only)" }
				: new PackageSignature
				{
					Signed = true,
					Method = "CMS detached signature over package-manifest.json (SHA-256) and an embedded PDF signature",
					ManifestSignatureFile = SignatureFile,
					PdfSigned = true,
					SignerSubject = signer.Subject,
					SignerIssuer = signer.Issuer,
					SignerThumbprint = signer.Thumbprint,
					SignerNotAfter = signer.NotAfter.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
				}
		};
		foreach (var (name, role) in new[] { (RecordFile, "verification record (JSON)"), (PdfFile, "verification record (PDF)"), (ReadmeFile, "instructions") })
		{
			string path = Path.Combine(folder, name);
			manifest.Files.Add(new PackageFile { Name = name, Role = role, Bytes = new FileInfo(path).Length, Sha256 = Sha256File(path) });
		}
		string manifestText = CanonicalJson.ToPretty(manifest) + Environment.NewLine;
		byte[] manifestBytes = new UTF8Encoding(false).GetBytes(manifestText);
		TextFiles.WriteBytesOutside(evidencePath, Path.Combine(folder, ManifestFile), manifestBytes);
		if (signer != null)
		{
			var cms = new SignedCms(new ContentInfo(manifestBytes), detached: true);
			var cmsSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, signer)
			{
				DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"),
				IncludeOption = X509IncludeOption.EndCertOnly
			};
			cmsSigner.SignedAttributes.Add(new Pkcs9SigningTime(now.UtcDateTime));
			cms.ComputeSignature(cmsSigner, silent: false);
			TextFiles.WriteBytesOutside(evidencePath, Path.Combine(folder, SignatureFile), cms.Encode());
		}

		string zip = UniqueFile(folder + ".zip");
		if (!string.IsNullOrWhiteSpace(evidencePath))
		{
			OutputPathGuard.EnsureOutside(evidencePath, zip);
		}
		ZipFile.CreateFromDirectory(folder, zip, CompressionLevel.Optimal, includeBaseDirectory: true);
		return new PackageResult(Path.GetFullPath(folder), Path.GetFullPath(zip), code, signer != null);
	}

	public static string Utc(DateTimeOffset value) =>
		value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

	public static string Local(DateTimeOffset value) =>
		value.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

	/// <summary>The zone's name at that instant, e.g. Mountain Daylight Time, falling back to the stored ID.</summary>
	public static string ZoneName(DateTimeOffset value, string? timeZoneId)
	{
		try
		{
			TimeZoneInfo zone = string.IsNullOrWhiteSpace(timeZoneId) ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
			return zone.IsDaylightSavingTime(value) ? zone.DaylightName : zone.StandardName;
		}
		catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
		{
			return timeZoneId ?? "";
		}
	}

	/// <summary>Local time with offset and zone name, e.g. 2026-10-06T05:41:32-06:00 (Mountain Daylight Time).</summary>
	public static string LocalWithZone(DateTimeOffset value, string? timeZoneId) =>
		Local(value) + " (" + ZoneName(value, timeZoneId) + ")";

	public static string Sha256File(string path)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
	}

	/// <summary>Certificates in the current user's store that can sign: private key present, valid now, digital-signature usage.</summary>
	public static IReadOnlyList<X509Certificate2> SigningCertificates()
	{
		var list = new List<X509Certificate2>();
		try
		{
			using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
			DateTime now = DateTime.Now;
			foreach (X509Certificate2 cert in store.Certificates)
			{
				if (!cert.HasPrivateKey || cert.NotBefore > now || cert.NotAfter < now)
				{
					continue;
				}
				var ku = cert.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
				if (ku != null && (ku.KeyUsages & (X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation)) == 0)
				{
					continue;
				}
				list.Add(cert);
			}
		}
		catch (CryptographicException)
		{
		}
		return list.OrderBy(c => c.GetNameInfo(X509NameType.SimpleName, false), StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string Readme(VerificationRecord record, PackageStamp stamp)
	{
		var sb = new StringBuilder();
		sb.AppendLine("DEVI Validate validation package");
		sb.AppendLine("================================");
		sb.AppendLine();
		sb.AppendLine("Verification code: " + stamp.VerificationCode);
		sb.AppendLine("Record SHA-256:    " + stamp.RecordSha256);
		sb.AppendLine("Verdict:           " + record.Verdict);
		sb.AppendLine("Case or reference: " + (record.CaseReference ?? "Not recorded"));
		sb.AppendLine("Examiner:          " + (record.Examiner ?? "Not recorded"));
		sb.AppendLine("Agency/department: " + (record.Agency ?? "Not recorded"));
		sb.AppendLine("Verified (UTC):    " + Utc(record.VerifiedAt));
		sb.AppendLine("Verified (local):  " + LocalWithZone(record.VerifiedAt, record.TimeZone));
		sb.AppendLine("Package (UTC):     " + Utc(stamp.CreatedAt));
		sb.AppendLine("Package (local):   " + LocalWithZone(stamp.CreatedAt, stamp.TimeZone));
		sb.AppendLine("Tool:              " + ToolInfo.Name + " " + ToolInfo.Version);
		sb.AppendLine("Executable SHA-256: " + (stamp.ExecutableSha256 ?? "Not available"));
		sb.AppendLine("Signed:            " + (stamp.SignerSubject == null ? "No. Hash-sealed only." : "Yes, by " + stamp.SignerSubject + " (thumbprint " + stamp.SignerThumbprint + ")"));
		sb.AppendLine();
		sb.AppendLine("Files");
		sb.AppendLine("  " + RecordFile + "   the verification record. Its integrity.hash is the record SHA-256.");
		sb.AppendLine("  " + PdfFile + "    the same record as a PDF, with the verification code and QR code.");
		sb.AppendLine("  " + ManifestFile + "      the SHA-256 of every file in this package.");
		if (stamp.SignerSubject != null)
		{
			sb.AppendLine("  " + SignatureFile + "  detached CMS signature over " + ManifestFile + ".");
		}
		sb.AppendLine();
		sb.AppendLine("How to verify");
		sb.AppendLine(HowToVerifyText);
		sb.AppendLine();
		sb.AppendLine("A package shows that these files have not changed since it was written. It does not certify a laboratory, a method, or a case.");
		return sb.ToString();
	}

	private static string UniqueFolder(string directory, string stem)
	{
		string path = Path.Combine(directory, stem);
		for (int i = 2; Directory.Exists(path) || File.Exists(path + ".zip"); i++)
		{
			path = Path.Combine(directory, stem + "-" + i.ToString(CultureInfo.InvariantCulture));
		}
		return path;
	}

	private static string UniqueFile(string path)
	{
		string candidate = path;
		for (int i = 2; File.Exists(candidate); i++)
		{
			candidate = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "-" + i.ToString(CultureInfo.InvariantCulture) + Path.GetExtension(path));
		}
		return candidate;
	}
}
