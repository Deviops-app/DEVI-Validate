using System;

namespace DeviValidate.Core.Hashing;

/// <summary>Progress for the file currently being read. Memory use stays at the buffer size.</summary>
public readonly record struct HashProgress(string RelativePath, long BytesRead, long TotalBytes, int FileIndex, int FileCount)
{
	public double Fraction
	{
		get
		{
			if (TotalBytes <= 0)
			{
				return 1.0;
			}
			return Math.Min(1.0, (double)BytesRead / (double)TotalBytes);
		}
	}
}
