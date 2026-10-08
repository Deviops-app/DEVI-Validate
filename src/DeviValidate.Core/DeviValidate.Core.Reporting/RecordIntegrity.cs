using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DeviValidate.Core.Reporting;

/// <summary>
/// SHA-256 of the canonical JSON for a record, computed with <c>integrity.hash</c> set to an empty string.
/// The hash covers that JSON. It does not cover the HTML page.
/// </summary>
public static class RecordIntegrity
{
	public static string Scope { get; } = "SHA-256 of the UTF-8 canonical JSON for this record, with integrity.hash set to an empty string. Canonical JSON is compact, has no byte-order mark, and uses the property order and string encoding written by DEVI Validate 1.0.0. The hash covers that JSON, not the HTML page.";


	public static void Stamp(IStampedRecord record)
	{
		if (record.Integrity == null)
		{
			ReportIntegrity reportIntegrity2 = (record.Integrity = new ReportIntegrity());
		}
		record.Integrity.Algorithm = "SHA-256";
		record.Integrity.Scope = Scope;
		record.Integrity.Hash = "";
		record.Integrity.Hash = Hash(record);
	}

	public static bool Matches(IStampedRecord record)
	{
		if (record.Integrity == null || string.IsNullOrEmpty(record.Integrity.Hash))
		{
			return false;
		}
		string hash = record.Integrity.Hash;
		record.Integrity.Hash = "";
		try
		{
			string s = Hash(record);
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			byte[] bytes2 = Encoding.ASCII.GetBytes(hash.ToLowerInvariant());
			return bytes.Length == bytes2.Length && CryptographicOperations.FixedTimeEquals(bytes, bytes2);
		}
		finally
		{
			record.Integrity.Hash = hash;
		}
	}

	public static void EnsureMatches(IStampedRecord record, string label)
	{
		if (!Matches(record))
		{
			throw new InvalidDataException("The " + label + " integrity hash does not match the record. The file may have been edited after it was written.");
		}
	}

	private static string Hash(IStampedRecord record)
	{
		return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record, record.GetType(), CanonicalJson.Compact))).ToLowerInvariant();
	}
}
