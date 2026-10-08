using System.IO;
using System.Linq;
using DeviValidate.Core;
using DeviValidate.Core.Expected;
using DeviValidate.Core.Hashing;
using Xunit;

namespace DeviValidate.Core.Tests;

public sealed class ExpectedHashInputTests
{
	[Fact]
	public void File_picked_then_cleared_is_empty()
	{
		ExpectedHashInput input = new ExpectedHashInput();
		Assert.True(input.AddFile(SamplePath(), SampleHashes(), "CSV hash list", keepExistingSource: false));

		Assert.Equal("hashes.csv", input.Files[0].ChipName);
		Assert.True(input.CanClear);
		Assert.True(input.FieldIsEditable);

		input.Clear();

		Assert.True(input.IsEmpty);
		Assert.Empty(input.Files);
		Assert.Equal("", input.PastedText);
		Assert.Null(input.SuggestedSource);
		Assert.False(input.HasComparison);
		Assert.False(input.CanClear);
		Assert.True(input.FieldIsEditable);
		Assert.Null(input.ForVerification());
	}

	[Fact]
	public void Typed_text_then_cleared_is_empty()
	{
		ExpectedHashInput input = new ExpectedHashInput();
		input.SetPastedText("abcd");

		Assert.True(input.CanClear);
		Assert.Equal("abcd", input.PastedText);

		input.Clear();

		Assert.Equal("", input.PastedText);
		Assert.True(input.IsEmpty);
		Assert.False(input.CanClear);
	}

	[Fact]
	public void Escape_clears_only_the_expected_hash_field()
	{
		ExpectedHashInput expected = new ExpectedHashInput();
		expected.AddFile(SamplePath(), SampleHashes(), "CSV hash list", keepExistingSource: false);
		expected.SetPastedText("abcd");
		PathSelection evidence = new PathSelection();
		evidence.Add(new[] { "one.bin", "two.bin" });

		Assert.True(expected.HandleEscape());
		Assert.True(expected.IsEmpty);
		Assert.Null(expected.ForVerification());
		Assert.Equal(new[] { "one.bin", "two.bin" }, evidence.Paths);
		Assert.False(expected.HandleEscape());

		Assert.True(evidence.HandleEscape());
		Assert.Empty(evidence.Paths);
		Assert.True(expected.IsEmpty);
	}

	[Fact]
	public void Removing_the_only_chip_resets_the_source()
	{
		ExpectedHashInput input = new ExpectedHashInput();
		ExpectedHashes parsed = SampleHashes();
		input.AddFile(SamplePath(), parsed, "CSV hash list", keepExistingSource: false);
		input.NoteCompared();

		Assert.Equal("CSV hash list", input.SuggestedSource);
		Assert.True(input.HasComparison);
		Assert.Same(parsed, input.ForVerification()!.Files[0].Parsed);

		Assert.True(input.RemoveFile(SamplePath()));

		Assert.Null(input.SuggestedSource);
		Assert.Empty(input.Files);
		Assert.False(input.HasComparison);
		Assert.Equal("", input.PastedText);
		Assert.Null(input.ForVerification());
		Assert.False(input.RemoveFile(SamplePath()));
	}

	[Fact]
	public void Cleared_state_yields_no_expected_hash_for_verification()
	{
		ExpectedHashInput input = new ExpectedHashInput();
		ExpectedHashes parsed = SampleHashes();
		input.AddFile(SamplePath(), parsed, "CSV hash list", keepExistingSource: false);
		input.NoteCompared();

		ExpectedHashVerificationInput ready = input.ForVerification()!;
		Assert.Equal(SamplePath(), ready.Files[0].Path);
		Assert.Same(parsed, ready.Files[0].Parsed);

		input.Clear();

		Assert.Null(input.ForVerification());
		Assert.False(input.HasComparison);

		input.SetPastedText("  abcd  ");
		Assert.Equal("abcd", input.ForVerification()!.PastedText);
		input.Clear();
		Assert.Null(input.ForVerification());
	}

	[Fact]
	public void Pasted_text_stays_beside_chosen_hash_files()
	{
		ExpectedHashInput input = new ExpectedHashInput();
		ExpectedHashes parsed = SampleHashes();
		input.AddFile(SamplePath(), parsed, "CSV hash list", keepExistingSource: false);
		input.NoteCompared();

		input.SetPastedText("abcd");

		Assert.True(input.FieldIsEditable);
		Assert.False(input.HasComparison);
		Assert.Equal("CSV hash list", input.SuggestedSource);
		Assert.Equal("hashes.csv", Assert.Single(input.Files).ChipName);
		ExpectedHashVerificationInput ready = input.ForVerification()!;
		Assert.Equal("abcd", ready.PastedText);
		Assert.Same(parsed, Assert.Single(ready.Files).Parsed);
	}

	[Fact]
	public void Adding_several_files_and_removing_one_keeps_the_rest_and_the_paste()
	{
		ExpectedHashInput input = new ExpectedHashInput();
		string first = Path.Combine("lab", "a.csv");
		string second = Path.Combine("lab", "b.csv");
		string third = Path.Combine("lab", "c.csv");
		ExpectedHashes firstHashes = SampleHashes();
		ExpectedHashes secondHashes = SampleHashes();
		ExpectedHashes thirdHashes = SampleHashes();

		Assert.Equal(3, input.AddFiles(new[] { first, second, third }, (string path) =>
		{
			if (path == first)
			{
				return (firstHashes, "CSV hash list");
			}
			if (path == second)
			{
				return (secondHashes, (string?)null);
			}
			return (thirdHashes, "Sum file");
		}, keepExistingSource: false));
		input.SetPastedText("abcd");
		input.NoteCompared();

		Assert.False(input.AddFile(second, secondHashes, null, keepExistingSource: true));
		Assert.Equal(3, input.Files.Count);
		Assert.True(input.HasComparison);
		Assert.Equal("CSV hash list", input.SuggestedSource);

		Assert.True(input.RemoveFile(second));

		Assert.Equal(new[] { first, third }, input.Files.Select((ExpectedHashFile file) => file.Path));
		Assert.Same(firstHashes, input.Files[0].Parsed);
		Assert.Same(thirdHashes, input.Files[1].Parsed);
		Assert.Equal("abcd", input.PastedText);
		Assert.Equal("CSV hash list", input.SuggestedSource);
		Assert.False(input.HasComparison);
		ExpectedHashVerificationInput ready = input.ForVerification()!;
		Assert.Equal(new[] { first, third }, ready.Files.Select((ExpectedHashFile file) => file.Path));
		Assert.Equal("abcd", ready.PastedText);
		Assert.DoesNotContain(ready.Files, (ExpectedHashFile file) => file.Parsed == secondHashes);
	}

	[Fact]
	public void Removing_the_file_that_suggested_the_lab_procedure_uses_the_next_one()
	{
		ExpectedHashInput input = new ExpectedHashInput();
		string first = Path.Combine("lab", "a.csv");
		string second = Path.Combine("lab", "b.sum");
		input.AddFile(first, SampleHashes(), "CSV hash list", keepExistingSource: false);
		input.AddFile(second, SampleHashes(), "Sum file", keepExistingSource: true);

		Assert.Equal("CSV hash list", input.SuggestedSource);
		Assert.True(input.RemoveFile(first));
		Assert.Equal("Sum file", input.SuggestedSource);
		Assert.Equal(second, Assert.Single(input.Files).Path);
	}

	private static string SamplePath()
	{
		return Path.Combine("lab", "hashes.csv");
	}

	private static ExpectedHashes SampleHashes()
	{
		return new ExpectedHashes
		{
			FormatName = "CSV",
			Algorithm = HashAlgorithmKind.Sha256,
			Entries = new[]
			{
				new ExpectedHashEntry
				{
					Path = "evidence.bin",
					Hash = new string('a', 64),
					Algorithm = HashAlgorithmKind.Sha256
				}
			}
		};
	}
}
