using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Expected;

public static class DeviManifestParser
{
	public const string ManifestFormat = "DEVI Validate manifest";

	public const string VerificationFormat = "DEVI Validate verification record";

	public static ExpectedHashes Parse(string json, HashAlgorithmKind? requested)
	{
		JsonDocument jsonDocument;
		try
		{
			jsonDocument = JsonDocument.Parse(json);
		}
		catch (JsonException ex)
		{
			throw new InvalidDataException("Could not read the JSON record: " + ex.Message, ex);
		}
		using (jsonDocument)
		{
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object || !rootElement.TryGetProperty("kind", out var value))
			{
				throw new InvalidDataException("JSON is not a DEVI Validate manifest or verification record.");
			}
			string @string = value.GetString();
			if (rootElement.TryGetProperty("tool", out var value2) && value2.GetString() != "DEVI Validate")
			{
				throw new InvalidDataException("JSON tool name is not DEVI Validate.");
			}
			if (!rootElement.TryGetProperty("algorithm", out var value3))
			{
				throw new InvalidDataException("JSON record does not include an algorithm.");
			}
			HashAlgorithmKind algorithm = HashAlgorithms.Parse(value3.GetString());
			if (requested.HasValue)
			{
				HashAlgorithmKind valueOrDefault = requested.GetValueOrDefault();
				if (valueOrDefault != algorithm)
				{
					throw new InvalidDataException($"The JSON record is {HashAlgorithms.DisplayName(algorithm)}, and --algorithm is {HashAlgorithms.DisplayName(valueOrDefault)}.");
				}
			}
			if (@string == "hash-manifest")
			{
				HashManifest hashManifest = CanonicalJson.FromJson<HashManifest>(json);
				RecordIntegrity.EnsureMatches(hashManifest, "manifest");
				List<ExpectedHashEntry> entries = hashManifest.Files.Select((FileHashEntry file) => new ExpectedHashEntry
				{
					Path = PathKeys.Normalize(file.Path),
					Hash = HashAlgorithms.NormalizeHex(file.Hash),
					Algorithm = algorithm,
					Size = file.Size
				}).ToList();
				return ExpectedHashSelection.Strict("DEVI Validate manifest", null, entries, algorithm);
			}
			if (@string == "verification-record")
			{
				VerificationRecord verificationRecord = CanonicalJson.FromJson<VerificationRecord>(json);
				RecordIntegrity.EnsureMatches(verificationRecord, "verification record");
				List<ExpectedHashEntry> list = new List<ExpectedHashEntry>();
				foreach (FileVerificationEntry file in verificationRecord.Files)
				{
					if (!(file.Result == "extra") && !string.IsNullOrEmpty(file.ExpectedHash))
					{
						list.Add(new ExpectedHashEntry
						{
							Path = PathKeys.Normalize(file.Path),
							Hash = HashAlgorithms.NormalizeHex(file.ExpectedHash),
							Algorithm = algorithm,
							Size = file.ExpectedSize
						});
					}
				}
				return ExpectedHashSelection.Strict("DEVI Validate verification record", verificationRecord.Note, list, algorithm);
			}
			throw new InvalidDataException("JSON kind must be hash-manifest or verification-record.");
		}
	}
}
