namespace Devi.Updates;

public sealed class UpdateException : Exception
{
	/// <summary>True when the host did not return a feed, so another allowed host may be tried.</summary>
	public bool IsTransport { get; }

	public UpdateException(string message, bool transport = false)
		: base(message)
	{
		IsTransport = transport;
	}
}
