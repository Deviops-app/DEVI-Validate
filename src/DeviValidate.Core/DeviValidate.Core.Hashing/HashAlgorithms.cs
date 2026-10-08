using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DeviValidate.Core.Hashing;

public static class HashAlgorithms
{
	public const int BufferSize = 1048576;

	public static HashAlgorithmKind Default => HashAlgorithmKind.Sha256;

	public static string DisplayName(HashAlgorithmKind kind)
	{
		return kind switch
		{
			HashAlgorithmKind.Sha256 => "SHA-256", 
			HashAlgorithmKind.Sha1 => "SHA-1", 
			HashAlgorithmKind.Md5 => "MD5", 
			_ => throw new ArgumentOutOfRangeException("kind"), 
		};
	}

	public static int HexLength(HashAlgorithmKind kind)
	{
		return kind switch
		{
			HashAlgorithmKind.Sha256 => 64, 
			HashAlgorithmKind.Sha1 => 40, 
			HashAlgorithmKind.Md5 => 32, 
			_ => throw new ArgumentOutOfRangeException("kind"), 
		};
	}

	public static HashAlgorithmName ToName(HashAlgorithmKind kind)
	{
		return kind switch
		{
			HashAlgorithmKind.Sha256 => HashAlgorithmName.SHA256, 
			HashAlgorithmKind.Sha1 => HashAlgorithmName.SHA1, 
			HashAlgorithmKind.Md5 => HashAlgorithmName.MD5, 
			_ => throw new ArgumentOutOfRangeException("kind"), 
		};
	}

	public static bool TryParse(string? text, out HashAlgorithmKind kind)
	{
		kind = HashAlgorithmKind.Sha256;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		switch (new string((from c in text.Trim().ToLowerInvariant()
			where c != '-' && c != '_' && c != ' '
			select c).ToArray()))
		{
		case "sha256":
			kind = HashAlgorithmKind.Sha256;
			return true;
		case "sha1":
			kind = HashAlgorithmKind.Sha1;
			return true;
		case "md5":
			kind = HashAlgorithmKind.Md5;
			return true;
		default:
			return false;
		}
	}

	public static HashAlgorithmKind Parse(string? text)
	{
		if (!TryParse(text, out var kind))
		{
			throw new InvalidDataException("Algorithm must be sha256, sha1, or md5.");
		}
		return kind;
	}

	public static bool TryFromHexLength(int length, out HashAlgorithmKind kind)
	{
		switch (length)
		{
		case 64:
			kind = HashAlgorithmKind.Sha256;
			return true;
		case 40:
			kind = HashAlgorithmKind.Sha1;
			return true;
		case 32:
			kind = HashAlgorithmKind.Md5;
			return true;
		default:
			kind = HashAlgorithmKind.Sha256;
			return false;
		}
	}

	public static HashAlgorithmKind FromHexLength(string hash)
	{
		if (!TryFromHexLength(hash.Length, out var kind))
		{
			throw new InvalidDataException($"Hash length {hash.Length} is not MD5, SHA-1, or SHA-256.");
		}
		return kind;
	}

	public static bool IsHex(string text)
	{
		if (text.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (!IsHexChar(text[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsHexChar(char c)
	{
		switch (c)
		{
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
			return true;
		default:
			return false;
		}
	}

	public static string NormalizeHex(string hash)
	{
		string text = hash.Trim();
		if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = text;
			text = text2.Substring(2, text2.Length - 2);
		}
		if (!IsHex(text))
		{
			throw new InvalidDataException("'" + hash + "' is not a hexadecimal hash.");
		}
		return text.ToLowerInvariant();
	}

	public static bool FixedTimeEquals(string left, string right)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(left);
		byte[] bytes2 = Encoding.ASCII.GetBytes(right);
		if (bytes.Length == bytes2.Length)
		{
			return CryptographicOperations.FixedTimeEquals(bytes, bytes2);
		}
		return false;
	}
}
