namespace Devi.Updates;

public sealed class UpdateProduct
{
	public string Id { get; }

	public string Version { get; }

	public string Released { get; }

	public string Notes { get; }

	public UpdateFile? Installer { get; }

	public UpdateFile? Portable { get; }

	public UpdateFile? PreferredFile => Installer ?? Portable;

	public UpdateProduct(string id, string version, string released, string notes, UpdateFile? installer, UpdateFile? portable)
	{
		Id = id;
		Version = version;
		Released = released;
		Notes = notes;
		Installer = installer;
		Portable = portable;
	}
}
