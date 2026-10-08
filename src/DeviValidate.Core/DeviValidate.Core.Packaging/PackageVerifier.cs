using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Packaging;

public enum CheckOutcome
{
	Pass,
	Fail,
	Warning,
	Info
}

public sealed record PackageCheck(string Name, CheckOutcome Outcome, string Detail);

public sealed class PackageVerification
{
	public List<PackageCheck> Checks { get; } = new List<PackageCheck>();

	public PackageManifest? Manifest { get; set; }

	public string Source { get; set; } = "";

	public bool Passed => Checks.Count > 0 && Checks.All(c => c.Outcome != CheckOutcome.Fail);

	public string Summary => Passed
		? "Package verified. Every file matches the manifest and the verification code matches the record."
		: "Package did not verify. See the failed checks.";

	internal void Add(string name, CheckOutcome outcome, string detail) => Checks.Add(new PackageCheck(name, outcome, detail));
}

/// <summary>Re-verifies a validation package offline. Reads only; never changes the package.</summary>
public static class PackageVerifier
{
	/// <param name="path">The package folder, its .zip, or its package-manifest.json.</param>
	/// <param name="enteredCode">Optional code from the printed PDF or a QR scan: the DV1 code, the full record SHA-256, or the QR text.</param>
	public static PackageVerification Verify(string path, string? enteredCode)
	{
		var result = new PackageVerification { Source = Path.GetFullPath(path) };
		string? temp = null;
		try
		{
			string folder;
			if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			{
				temp = Path.Combine(Path.GetTempPath(), "devi-validate-verify-" + Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(temp);
				ExtractSafely(path, temp);
				string? manifestPath = Directory.EnumerateFiles(temp, ValidationPackage.ManifestFile, SearchOption.AllDirectories).FirstOrDefault();
				if (manifestPath == null)
				{
					result.Add("Manifest", CheckOutcome.Fail, "The .zip does not contain " + ValidationPackage.ManifestFile + ".");
					return result;
				}
				folder = Path.GetDirectoryName(manifestPath)!;
			}
			else if (File.Exists(path))
			{
				folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
			}
			else if (Directory.Exists(path))
			{
				folder = path;
			}
			else
			{
				result.Add("Package", CheckOutcome.Fail, "Nothing is at that path.");
				return result;
			}
			VerifyFolder(folder, enteredCode, result);
			return result;
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
		{
			result.Add("Package", CheckOutcome.Fail, "Could not read the package: " + ex.Message);
			return result;
		}
		finally
		{
			if (temp != null)
			{
				try { Directory.Delete(temp, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
			}
		}
	}

	private static void VerifyFolder(string folder, string? enteredCode, PackageVerification result)
	{
		string manifestPath = Path.Combine(folder, ValidationPackage.ManifestFile);
		if (!File.Exists(manifestPath))
		{
			result.Add("Manifest", CheckOutcome.Fail, ValidationPackage.ManifestFile + " is missing.");
			return;
		}
		byte[] manifestBytes = File.ReadAllBytes(manifestPath);
		PackageManifest manifest;
		try
		{
			manifest = CanonicalJson.FromJson<PackageManifest>(Encoding.UTF8.GetString(manifestBytes));
		}
		catch (JsonException ex)
		{
			result.Add("Manifest", CheckOutcome.Fail, "The manifest is not valid JSON: " + ex.Message);
			return;
		}
		if (manifest.Kind != PackageManifest.KindValue || manifest.SchemaVersion != 1)
		{
			result.Add("Manifest", CheckOutcome.Fail, "This is not a DEVI Validate package manifest (kind " + manifest.Kind + ", schema " + manifest.SchemaVersion + ").");
			return;
		}
		result.Manifest = manifest;
		result.Add("Manifest", CheckOutcome.Pass, "Written by " + manifest.Tool + " " + manifest.Version + " at " + manifest.CreatedAtUtc + " (" + LocalLabel(manifest.CreatedAtLocal, manifest.TimeZone) + ").");

		var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ValidationPackage.ManifestFile, ValidationPackage.SignatureFile };
		foreach (PackageFile file in manifest.Files)
		{
			listed.Add(file.Name);
			if (file.Name.Contains('/') || file.Name.Contains('\\') || file.Name.Contains(".."))
			{
				result.Add(file.Name, CheckOutcome.Fail, "The manifest names a file outside the package.");
				continue;
			}
			string path = Path.Combine(folder, file.Name);
			if (!File.Exists(path))
			{
				result.Add(file.Name, CheckOutcome.Fail, "Listed in the manifest but missing.");
				continue;
			}
			string actual = ValidationPackage.Sha256File(path);
			bool same = string.Equals(actual, file.Sha256, StringComparison.OrdinalIgnoreCase);
			result.Add(file.Name, same ? CheckOutcome.Pass : CheckOutcome.Fail,
				same ? "SHA-256 matches: " + actual : "SHA-256 is " + actual + ", the manifest says " + file.Sha256 + ". The file changed after the package was written.");
		}
		foreach (string extra in Directory.EnumerateFiles(folder).Select(Path.GetFileName).Where(n => n != null && !listed.Contains(n!))!)
		{
			result.Add(extra!, CheckOutcome.Warning, "In the folder but not listed in the manifest. It is not covered by the package.");
		}

		string recordPath = Path.Combine(folder, ValidationPackage.RecordFile);
		if (File.Exists(recordPath))
		{
			try
			{
				VerificationRecord record = CanonicalJson.FromJson<VerificationRecord>(File.ReadAllText(recordPath));
				bool intact = RecordIntegrity.Matches(record);
				result.Add("Record hash", intact ? CheckOutcome.Pass : CheckOutcome.Fail,
					intact ? "integrity.hash recomputes from the record JSON." : "integrity.hash does not recompute. The record was edited after it was written.");
				bool same = string.Equals(record.Integrity?.Hash, manifest.RecordSha256, StringComparison.OrdinalIgnoreCase);
				result.Add("Record and manifest", same ? CheckOutcome.Pass : CheckOutcome.Fail,
					same ? "recordSha256 in the manifest is the record's integrity hash." : "The manifest names a different record hash.");
			}
			catch (JsonException ex)
			{
				result.Add("Record hash", CheckOutcome.Fail, "The record is not valid JSON: " + ex.Message);
			}
		}

		bool codeOk = false;
		try
		{
			codeOk = string.Equals(VerificationCode.FromRecordHash(manifest.RecordSha256), manifest.VerificationCode, StringComparison.Ordinal)
				&& string.Equals(VerificationCode.QrPayload(manifest.RecordSha256), manifest.QrPayload, StringComparison.Ordinal);
		}
		catch (ArgumentException)
		{
		}
		result.Add("Verification code", codeOk ? CheckOutcome.Pass : CheckOutcome.Fail,
			codeOk ? manifest.VerificationCode + " comes from the record hash." : "The verification code in the manifest does not come from the record hash.");

		if (!string.IsNullOrWhiteSpace(enteredCode))
		{
			bool match = VerificationCode.Matches(enteredCode, manifest.RecordSha256);
			result.Add("Code you entered", match ? CheckOutcome.Pass : CheckOutcome.Fail,
				match ? "Matches this package. The printed PDF and these files describe the same record." : "Does not match this package (expected " + manifest.VerificationCode + ").");
		}

		string sigPath = Path.Combine(folder, ValidationPackage.SignatureFile);
		if (File.Exists(sigPath))
		{
			VerifySignature(manifestBytes, File.ReadAllBytes(sigPath), manifest, result);
		}
		else if (manifest.Signature.Signed)
		{
			result.Add("Signature", CheckOutcome.Fail, "The manifest says the package is signed, but " + ValidationPackage.SignatureFile + " is missing.");
		}
		else
		{
			result.Add("Signature", CheckOutcome.Info, "Not signed. The package is hash-sealed only: the file hashes show the files are unchanged, but no certificate vouches for who wrote it.");
		}

		string pdfPath = Path.Combine(folder, ValidationPackage.PdfFile);
		if (File.Exists(pdfPath) && manifest.Signature.PdfSigned)
		{
			string pdfText = Encoding.Latin1.GetString(File.ReadAllBytes(pdfPath));
			bool hasSig = pdfText.Contains("/ByteRange", StringComparison.Ordinal) && pdfText.Contains("/Sig", StringComparison.Ordinal);
			result.Add("PDF signature", hasSig ? CheckOutcome.Info : CheckOutcome.Fail,
				hasSig ? "The PDF carries an embedded signature. Open it in a PDF reader to see the signature panel. Its bytes are covered by the manifest hash above." : "The manifest says the PDF is signed, but no signature was found in it.");
		}
	}

	private static string LocalLabel(string local, string timeZone)
	{
		return DateTimeOffset.TryParse(local, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTimeOffset value)
			? local + " " + ValidationPackage.ZoneName(value, timeZone)
			: local + " " + timeZone;
	}

	private static void VerifySignature(byte[] manifestBytes, byte[] signature, PackageManifest manifest, PackageVerification result)
	{
		try
		{
			var cms = new SignedCms(new ContentInfo(manifestBytes), detached: true);
			cms.Decode(signature);
			cms.CheckSignature(verifySignatureOnly: true);
			X509Certificate2? cert = cms.SignerInfos.Count > 0 ? cms.SignerInfos[0].Certificate : null;
			string who = cert == null ? "an unknown signer" : cert.Subject + " (thumbprint " + cert.Thumbprint + ")";
			result.Add("Signature", CheckOutcome.Pass, "The manifest signature is valid. Signed by " + who + ".");
			if (cert != null && manifest.Signature.SignerThumbprint != null && !string.Equals(cert.Thumbprint, manifest.Signature.SignerThumbprint, StringComparison.OrdinalIgnoreCase))
			{
				result.Add("Signer", CheckOutcome.Fail, "The signing certificate is not the one the manifest names.");
			}
			try
			{
				cms.CheckSignature(verifySignatureOnly: false);
				result.Add("Certificate trust", CheckOutcome.Pass, "The signer's certificate chains to a root this computer trusts.");
			}
			catch (CryptographicException ex)
			{
				result.Add("Certificate trust", CheckOutcome.Warning, "The signature is valid, but this computer does not trust the signer's certificate chain: " + ex.Message.Trim());
			}
		}
		catch (CryptographicException ex)
		{
			result.Add("Signature", CheckOutcome.Fail, "The manifest signature does not verify: " + ex.Message.Trim());
		}
	}

	private static void ExtractSafely(string zipPath, string destination)
	{
		string root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
		using ZipArchive archive = ZipFile.OpenRead(zipPath);
		long total = 0;
		foreach (ZipArchiveEntry entry in archive.Entries)
		{
			string target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
			if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("The .zip contains a path outside the package.");
			}
			total += entry.Length;
			if (total > 2L * 1024 * 1024 * 1024)
			{
				throw new InvalidDataException("The .zip is larger than a validation package should be.");
			}
			if (string.IsNullOrEmpty(entry.Name))
			{
				Directory.CreateDirectory(target);
				continue;
			}
			Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			entry.ExtractToFile(target, overwrite: false);
		}
	}
}
