namespace Devi.Updates;

public sealed class UpdateFile
{
	public string Url { get; }

	public string Sha256 { get; }

	public string Name { get; }

	public UpdateFile(string url, string sha256, string name)
	{
		Url = url;
		Sha256 = sha256.ToLowerInvariant();
		Name = name;
	}
}
