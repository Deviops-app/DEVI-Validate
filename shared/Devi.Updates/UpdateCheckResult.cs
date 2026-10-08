namespace Devi.Updates;

public sealed class UpdateCheckResult
{
	public UpdateStatus Status { get; }

	public string Message { get; }

	public UpdateProduct? Product { get; }

	public Version? RemoteVersion { get; }

	public UpdateCheckResult(UpdateStatus status, string message, UpdateProduct? product, Version? remoteVersion)
	{
		Status = status;
		Message = message;
		Product = product;
		RemoteVersion = remoteVersion;
	}
}
