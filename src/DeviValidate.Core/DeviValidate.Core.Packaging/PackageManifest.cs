using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeviValidate.Core.Packaging;

/// <summary>package-manifest.json: what is in a validation package and the SHA-256 of each file.</summary>
public sealed class PackageManifest
{
	public const string KindValue = "validation-package";

	[JsonPropertyName("schemaVersion")]
	public int SchemaVersion { get; set; } = 1;

	[JsonPropertyName("kind")]
	public string Kind { get; set; } = KindValue;

	[JsonPropertyName("tool")]
	public string Tool { get; set; } = ToolInfo.Name;

	[JsonPropertyName("version")]
	public string Version { get; set; } = ToolInfo.Version;

	[JsonPropertyName("executable")]
	public string? Executable { get; set; }

	/// <summary>SHA-256 of the executable that wrote the package, when it could be read.</summary>
	[JsonPropertyName("executableSha256")]
	public string? ExecutableSha256 { get; set; }

	/// <summary>SHA-256 of DeviValidate.Core.dll when it is a separate file (not in single-file builds).</summary>
	[JsonPropertyName("coreSha256")]
	public string? CoreSha256 { get; set; }

	[JsonPropertyName("createdAtUtc")]
	public string CreatedAtUtc { get; set; } = "";

	[JsonPropertyName("createdAtLocal")]
	public string CreatedAtLocal { get; set; } = "";

	[JsonPropertyName("timeZone")]
	public string TimeZone { get; set; } = "";

	[JsonPropertyName("machineName")]
	public string MachineName { get; set; } = "";

	[JsonPropertyName("examiner")]
	public string? Examiner { get; set; }

	/// <summary>Agency or department. Written only when recorded, so records without it keep their canonical form.</summary>
	[JsonPropertyName("agency")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Agency { get; set; }

	[JsonPropertyName("caseReference")]
	public string? CaseReference { get; set; }

	[JsonPropertyName("validatedBy")]
	public string? ValidatedBy { get; set; }

	[JsonPropertyName("validationDate")]
	public string? ValidationDate { get; set; }

	[JsonPropertyName("labProcedure")]
	public string? LabProcedure { get; set; }

	[JsonPropertyName("verifiedAtUtc")]
	public string VerifiedAtUtc { get; set; } = "";

	[JsonPropertyName("verifiedAtLocal")]
	public string VerifiedAtLocal { get; set; } = "";

	[JsonPropertyName("verdict")]
	public string Verdict { get; set; } = "";

	[JsonPropertyName("algorithm")]
	public string Algorithm { get; set; } = "";

	[JsonPropertyName("evidencePath")]
	public string EvidencePath { get; set; } = "";

	[JsonPropertyName("evidenceKind")]
	public string EvidenceKind { get; set; } = "";

	[JsonPropertyName("evidenceHash")]
	public string? EvidenceHash { get; set; }

	/// <summary>integrity.hash of verification-record.json: SHA-256 of the canonical record JSON.</summary>
	[JsonPropertyName("recordSha256")]
	public string RecordSha256 { get; set; } = "";

	[JsonPropertyName("verificationCode")]
	public string VerificationCode { get; set; } = "";

	[JsonPropertyName("qrPayload")]
	public string QrPayload { get; set; } = "";

	[JsonPropertyName("files")]
	public List<PackageFile> Files { get; set; } = new List<PackageFile>();

	[JsonPropertyName("signature")]
	public PackageSignature Signature { get; set; } = new PackageSignature();

	[JsonPropertyName("howToVerify")]
	public string HowToVerify { get; set; } = "";
}

public sealed class PackageFile
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("role")]
	public string Role { get; set; } = "";

	[JsonPropertyName("bytes")]
	public long Bytes { get; set; }

	[JsonPropertyName("sha256")]
	public string Sha256 { get; set; } = "";
}

public sealed class PackageSignature
{
	[JsonPropertyName("signed")]
	public bool Signed { get; set; }

	[JsonPropertyName("method")]
	public string Method { get; set; } = "none";

	[JsonPropertyName("manifestSignatureFile")]
	public string? ManifestSignatureFile { get; set; }

	[JsonPropertyName("pdfSigned")]
	public bool PdfSigned { get; set; }

	[JsonPropertyName("signerSubject")]
	public string? SignerSubject { get; set; }

	[JsonPropertyName("signerIssuer")]
	public string? SignerIssuer { get; set; }

	[JsonPropertyName("signerThumbprint")]
	public string? SignerThumbprint { get; set; }

	[JsonPropertyName("signerNotAfter")]
	public string? SignerNotAfter { get; set; }
}

/// <summary>What the PDF prints on its package page. Built before the PDF is written.</summary>
public sealed record PackageStamp(
	string VerificationCode,
	string RecordSha256,
	string QrPayload,
	DateTimeOffset CreatedAt,
	string TimeZone,
	string? Executable,
	string? ExecutableSha256,
	string? CoreSha256,
	string? SignerSubject,
	string? SignerThumbprint);
