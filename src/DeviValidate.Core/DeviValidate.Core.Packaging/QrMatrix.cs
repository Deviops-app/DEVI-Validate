using System.Collections.Generic;
using QRCoder;

namespace DeviValidate.Core.Packaging;

/// <summary>QR modules for the package payload. Drawn as vector squares by the PDF and the app.</summary>
public static class QrMatrix
{
	/// <summary>Rows of dark (true) and light (false) modules, quiet zone excluded.</summary>
	public static bool[][] Create(string payload)
	{
		using var generator = new QRCodeGenerator();
		using QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
		List<System.Collections.BitArray> matrix = data.ModuleMatrix;
		// QRCoder pads the matrix with a 4-module quiet zone on every side.
		const int quiet = 4;
		int size = matrix.Count - quiet * 2;
		var rows = new bool[size][];
		for (int y = 0; y < size; y++)
		{
			rows[y] = new bool[size];
			for (int x = 0; x < size; x++)
			{
				rows[y][x] = matrix[y + quiet][x + quiet];
			}
		}
		return rows;
	}
}
