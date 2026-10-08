using System;

namespace DeviValidate.Cli;

internal sealed class CliException : Exception
{
	public CliException(string message)
		: base(message)
	{
	}
}
