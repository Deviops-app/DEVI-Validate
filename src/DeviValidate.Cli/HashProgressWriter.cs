using System;
using System.Globalization;
using DeviValidate.Core;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Cli;

internal sealed class HashProgressWriter : IProgress<HashProgress>
{
	private readonly bool _quiet;

	private readonly bool _interactive;

	private long _lastTick;

	private int _width;

	private string? _completedPath;

	private bool _emptyStarted;

	public HashProgressWriter(bool quiet)
	{
		_quiet = quiet;
		_interactive = !Console.IsErrorRedirected;
	}

	public void Report(HashProgress value)
	{
		if (_quiet)
		{
			return;
		}
		bool num = value.TotalBytes <= 0;
		bool flag = !num && value.BytesRead >= value.TotalBytes;
		if (num)
		{
			if (!_emptyStarted)
			{
				_emptyStarted = true;
				flag = false;
			}
			else
			{
				flag = true;
				_emptyStarted = false;
			}
		}
		string value2 = ((num || value.TotalBytes <= 0) ? "100%" : ((value.Fraction * 100.0).ToString("0.0", CultureInfo.InvariantCulture) + "%"));
		string text = $"hashing ({value.FileIndex}/{value.FileCount}) {value.RelativePath}  {value2}  {ByteSize.Format(value.BytesRead)} / {ByteSize.Format(value.TotalBytes)}";
		if (_interactive)
		{
			long tickCount = Environment.TickCount64;
			if (flag || tickCount - _lastTick >= 200 || _width <= 0)
			{
				_lastTick = tickCount;
				if (text.Length > _width)
				{
					_width = text.Length;
				}
				Console.Error.Write("\r" + text.PadRight(_width));
				if (flag)
				{
					Console.Error.WriteLine();
					_width = 0;
				}
			}
		}
		else if (flag && _completedPath != value.RelativePath)
		{
			_completedPath = value.RelativePath;
			Console.Error.WriteLine(text);
		}
	}

	public void Finish()
	{
		if (_interactive && _width > 0)
		{
			Console.Error.WriteLine();
			_width = 0;
		}
	}
}
