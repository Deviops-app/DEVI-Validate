using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Devi.Updates;

public static class UpdateFeed
{
	public const int Schema = 1;

	public static string Canonical(IReadOnlyList<UpdateProduct> products)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(memoryStream))
		{
			utf8JsonWriter.WriteStartObject();
			utf8JsonWriter.WriteNumber("schema", 1);
			utf8JsonWriter.WriteStartArray("products");
			foreach (UpdateProduct product in products)
			{
				utf8JsonWriter.WriteStartObject();
				utf8JsonWriter.WriteString("id", product.Id);
				utf8JsonWriter.WriteString("version", product.Version);
				utf8JsonWriter.WriteString("released", product.Released);
				utf8JsonWriter.WriteString("notes", product.Notes);
				WriteFile(utf8JsonWriter, "installer", product.Installer);
				WriteFile(utf8JsonWriter, "portable", product.Portable);
				utf8JsonWriter.WriteEndObject();
			}
			utf8JsonWriter.WriteEndArray();
			utf8JsonWriter.WriteEndObject();
		}
		return Encoding.UTF8.GetString(memoryStream.ToArray());
	}

	public static string Sign(IReadOnlyList<UpdateProduct> products, ECDsa privateKey)
	{
		return Convert.ToBase64String(privateKey.SignData(Encoding.UTF8.GetBytes(Canonical(products)), HashAlgorithmName.SHA256));
	}

	public static IReadOnlyList<UpdateProduct> ParseAndVerify(string json, ECDsa publicKey)
	{
		var (readOnlyList, s) = Read(json, requireSignature: true);
		if (string.IsNullOrWhiteSpace(s))
		{
			throw new UpdateException("The update feed has no signature.");
		}
		byte[] signature;
		try
		{
			signature = Convert.FromBase64String(s);
		}
		catch (FormatException)
		{
			throw new UpdateException("The update feed signature is not valid.");
		}
		if (!publicKey.VerifyData(Encoding.UTF8.GetBytes(Canonical(readOnlyList)), signature, HashAlgorithmName.SHA256))
		{
			throw new UpdateException("The update feed signature does not match. The file was not accepted.");
		}
		return readOnlyList;
	}

	public static IReadOnlyList<UpdateProduct> ParseUnsigned(string json)
	{
		return Read(json, requireSignature: false).Products;
	}

	public static string ToSignedDocument(IReadOnlyList<UpdateProduct> products, string signature)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions
		{
			Indented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}))
		{
			utf8JsonWriter.WriteStartObject();
			utf8JsonWriter.WriteNumber("schema", 1);
			utf8JsonWriter.WriteString("signature", signature);
			utf8JsonWriter.WriteStartArray("products");
			foreach (UpdateProduct product in products)
			{
				utf8JsonWriter.WriteStartObject();
				utf8JsonWriter.WriteString("id", product.Id);
				utf8JsonWriter.WriteString("version", product.Version);
				utf8JsonWriter.WriteString("released", product.Released);
				utf8JsonWriter.WriteString("notes", product.Notes);
				WriteFile(utf8JsonWriter, "installer", product.Installer);
				WriteFile(utf8JsonWriter, "portable", product.Portable);
				utf8JsonWriter.WriteEndObject();
			}
			utf8JsonWriter.WriteEndArray();
			utf8JsonWriter.WriteEndObject();
		}
		return Encoding.UTF8.GetString(memoryStream.ToArray()) + "\n";
	}

	public static UpdateCheckResult Compare(UpdateProduct product, Version current)
	{
		if (!Version.TryParse(product.Version, out Version? result) || result is null)
		{
			throw new UpdateException("The published version is not a version number.");
		}
		int num = Normalize(result).CompareTo(Normalize(current));
		if (num > 0)
		{
			return new UpdateCheckResult(UpdateStatus.UpdateAvailable, "Version " + product.Version + " is available. This copy is " + current?.ToString() + ".", product, result);
		}
		if (num == 0)
		{
			return new UpdateCheckResult(UpdateStatus.UpToDate, "This copy is the published version " + current?.ToString() + ".", product, result);
		}
		return new UpdateCheckResult(UpdateStatus.NewerThanPublished, "This copy (" + current?.ToString() + ") is newer than the published version " + product.Version + ".", product, result);
	}

	public static UpdateProduct FindProduct(IReadOnlyList<UpdateProduct> products, string productId)
	{
		UpdateProduct? updateProduct = null;
		foreach (UpdateProduct product in products)
		{
			if (string.Equals(product.Id, productId, StringComparison.Ordinal))
			{
				if (updateProduct != null)
				{
					throw new UpdateException("The update feed lists " + productId + " more than once.");
				}
				updateProduct = product;
			}
		}
		if (updateProduct == null)
		{
			throw new UpdateException("The update feed has no release for " + productId + ".");
		}
		return updateProduct;
	}

	private static (IReadOnlyList<UpdateProduct> Products, string? Signature) Read(string json, bool requireSignature)
	{
		JsonDocument jsonDocument;
		try
		{
			jsonDocument = JsonDocument.Parse(json);
		}
		catch (JsonException ex)
		{
			throw new UpdateException("The update feed is not valid JSON. " + ex.Message);
		}
		using (jsonDocument)
		{
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object)
			{
				throw new UpdateException("The update feed is not a JSON object.");
			}
			if (!rootElement.TryGetProperty("schema", out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var value2) || value2 != 1)
			{
				throw new UpdateException("The update feed schema is not supported.");
			}
			string? text = null;
			if (rootElement.TryGetProperty("signature", out var value3))
			{
				if (value3.ValueKind != JsonValueKind.String)
				{
					throw new UpdateException("The update feed signature is not valid.");
				}
				text = value3.GetString();
			}
			if (requireSignature && string.IsNullOrWhiteSpace(text))
			{
				throw new UpdateException("The update feed has no signature.");
			}
			if (!rootElement.TryGetProperty("products", out var value4) || value4.ValueKind != JsonValueKind.Array)
			{
				throw new UpdateException("The update feed has no product list.");
			}
			List<UpdateProduct> list = new List<UpdateProduct>();
			foreach (JsonElement item in value4.EnumerateArray())
			{
				list.Add(ReadProduct(item));
			}
			if (list.Count == 0)
			{
				throw new UpdateException("The update feed has no products.");
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (UpdateProduct item2 in list)
			{
				if (!hashSet.Add(item2.Id))
				{
					throw new UpdateException("The update feed lists " + item2.Id + " more than once.");
				}
			}
			return (Products: list, Signature: text);
		}
	}

	private static UpdateProduct ReadProduct(JsonElement item)
	{
		string text = RequiredString(item, "id");
		string version = RequiredString(item, "version");
		string released = OptionalString(item, "released");
		string text2 = OptionalString(item, "notes");
		if (text2.Length > 4000)
		{
			throw new UpdateException("The release notes are too long.");
		}
		UpdateFile? updateFile = ReadFile(item, "installer", ".exe");
		UpdateFile? updateFile2 = ReadFile(item, "portable", ".zip");
		if (updateFile == null && updateFile2 == null)
		{
			throw new UpdateException("The release for " + text + " has no installer and no portable zip.");
		}
		return new UpdateProduct(text, version, released, text2, updateFile, updateFile2);
	}

	private static UpdateFile? ReadFile(JsonElement item, string name, string extension)
	{
		if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
		{
			return null;
		}
		if (value.ValueKind != JsonValueKind.Object)
		{
			throw new UpdateException("The " + name + " entry is not valid.");
		}
		string url = RequiredString(value, "url");
		string text = RequiredString(value, "sha256").ToLowerInvariant();
		string text2 = RequiredString(value, "name");
		if (text.Length != 64 || text.Any((char character) => !Uri.IsHexDigit(character)))
		{
			throw new UpdateException("The " + name + " SHA-256 is not 64 hex characters.");
		}
		if (!IsSingleFileName(text2) || !text2.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
		{
			throw new UpdateException("The " + name + " file name must be a single " + extension + " name.");
		}
		return new UpdateFile(url, text, text2);
	}

	private static bool IsSingleFileName(string fileName)
	{
		if (fileName != Path.GetFileName(fileName) || fileName.Contains("..", StringComparison.Ordinal))
		{
			return false;
		}
		bool flag = ((fileName == "." || fileName == "..") ? true : false);
		if (flag || fileName.EndsWith(' ') || fileName.EndsWith('.'))
		{
			return false;
		}
		foreach (char c in fileName)
		{
			flag = c < ' ';
			if (!flag)
			{
				bool flag2;
				switch (c)
				{
				case '"':
				case '*':
				case '/':
				case ':':
				case '<':
				case '>':
				case '?':
				case '\\':
				case '|':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				return false;
			}
		}
		return true;
	}

	private static Version Normalize(Version version)
	{
		return new Version(version.Major, (version.Minor >= 0) ? version.Minor : 0, (version.Build >= 0) ? version.Build : 0, (version.Revision >= 0) ? version.Revision : 0);
	}

	private static string RequiredString(JsonElement item, string name)
	{
		if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
		{
			throw new UpdateException("The update feed is missing " + name + ".");
		}
		string? obj = value.GetString() ?? "";
		if (string.IsNullOrWhiteSpace(obj))
		{
			throw new UpdateException("The update feed is missing " + name + ".");
		}
		return obj.Trim();
	}

	private static string OptionalString(JsonElement item, string name)
	{
		if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
		{
			return "";
		}
		if (value.ValueKind != JsonValueKind.String)
		{
			throw new UpdateException("The update feed field " + name + " is not text.");
		}
		return value.GetString()?.Trim() ?? "";
	}

	private static void WriteFile(Utf8JsonWriter writer, string name, UpdateFile? file)
	{
		if (file != null)
		{
			writer.WriteStartObject(name);
			writer.WriteString("url", file.Url);
			writer.WriteString("sha256", file.Sha256);
			writer.WriteString("name", file.Name);
			writer.WriteEndObject();
		}
	}
}
