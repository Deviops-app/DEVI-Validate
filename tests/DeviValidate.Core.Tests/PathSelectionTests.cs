using System.IO;
using DeviValidate.Core;
using Xunit;

namespace DeviValidate.Core.Tests;

public sealed class PathSelectionTests
{
	[Fact]
	public void Adding_several_paths_and_removing_one_keeps_the_rest_in_order()
	{
		PathSelection selection = new PathSelection();
		string first = Path.Combine("evidence", "one.bin");
		string second = Path.Combine("evidence", "two.bin");
		string third = Path.Combine("evidence", "three.bin");

		Assert.Equal(3, selection.Add(new[] { first, "  ", second, third }));
		Assert.Equal(new[] { first, second, third }, selection.Paths);
		Assert.Equal("two.bin", PathSelection.ChipName(second));

		Assert.True(selection.Remove(second));
		Assert.Equal(new[] { first, third }, selection.Paths);
		Assert.False(selection.Remove(second));
		Assert.Equal(2, selection.Paths.Count);
	}

	[Fact]
	public void A_duplicate_path_is_not_a_second_item()
	{
		PathSelection selection = new PathSelection();
		string first = Path.Combine("evidence", "one.bin");
		string second = Path.Combine("evidence", "two.bin");
		selection.Add(new[] { first });
		string samePath = "." + Path.DirectorySeparatorChar + first;

		Assert.Equal(1, selection.Add(new[] { first, samePath, second }));
		Assert.Equal(new[] { first, second }, selection.Paths);
	}

	[Fact]
	public void Escape_clears_only_that_selection()
	{
		PathSelection evidence = new PathSelection();
		PathSelection other = new PathSelection();
		evidence.Add(new[] { "one.bin", "two.bin" });
		other.Add(new[] { "hashes.csv" });

		Assert.False(new PathSelection().HandleEscape());
		Assert.True(evidence.HandleEscape());
		Assert.Empty(evidence.Paths);
		Assert.False(evidence.CanClear);
		Assert.False(evidence.HandleEscape());
		Assert.Equal(new[] { "hashes.csv" }, other.Paths);

		Assert.True(other.HandleEscape());
		Assert.Empty(other.Paths);
		Assert.Empty(evidence.Paths);
	}
}
