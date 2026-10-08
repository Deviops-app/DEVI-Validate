using DeviValidate.Core.Hashing;
using DeviValidate.Core.IO;

namespace DeviValidate.Core.Tests;

public class EvidenceTests
{
    [Fact]
    public async Task FolderHashUsesRelativePathsAndKnownDigest()
    {
        using var dir = new TempDir();
        dir.Write("notes.txt", "abc");
        dir.Write("photos/one.bin", "abc");
        var manifest = await Hash(dir.Path);

        Assert.Equal(new[] { "notes.txt", "photos/one.bin" }, manifest.Files.Select(file => file.Path).ToArray());
        Assert.All(manifest.Files, file => Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", file.Hash));
        Assert.Equal(3, manifest.Files[0].Size);
        Assert.False(string.IsNullOrWhiteSpace(manifest.Integrity.Hash));
    }

    [Fact]
    public async Task DoesNotChangeBytesOrWriteTimeAndDoesNotCreateFiles()
    {
        using var dir = new TempDir();
        var file = dir.Write("evidence.bin", "abc");
        var past = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(file, past);
        var beforeAccess = File.GetLastAccessTimeUtc(file);
        var beforeNames = Directory.GetFileSystemEntries(dir.Path, "*", SearchOption.AllDirectories).OrderBy(name => name).ToArray();

        await Hash(dir.Path);

        var afterAccess = File.GetLastAccessTimeUtc(file);
        var afterWrite = File.GetLastWriteTimeUtc(file);
        var afterNames = Directory.GetFileSystemEntries(dir.Path, "*", SearchOption.AllDirectories).OrderBy(name => name).ToArray();
        Assert.Equal("abc", File.ReadAllText(file));
        Assert.Equal(past, afterWrite);
        Assert.Equal(beforeAccess, afterAccess);
        Assert.Equal(beforeNames, afterNames);
    }

    [SymlinkFact]
    public async Task DoesNotFollowSymbolicLinks()
    {
        using var dir = new TempDir();
        var outside = dir.Write("outside.txt", "abc");
        var evidence = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "keep.txt"), "abc");
        LinkSupport.CreateFileSymlink(Path.Combine(evidence, "linked.txt"), outside);

        var manifest = await Hash(evidence);
        Assert.Equal(new[] { "keep.txt" }, manifest.Files.Select(file => file.Path).ToArray());
        var skipped = Assert.Single(manifest.Skipped);
        Assert.Equal("linked.txt", skipped.Path);
        Assert.Equal("symbolic link", skipped.Reason);
    }

    [SymlinkFact]
    public async Task RefusesASymbolicLinkAsTheEvidencePath()
    {
        using var dir = new TempDir();
        var target = dir.Write("target.txt", "abc");
        var link = Path.Combine(dir.Path, "link.txt");
        LinkSupport.CreateFileSymlink(link, target);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Hash(link));
    }

    [Fact]
    public async Task PreCanceledHashDoesNotOpenTheFile()
    {
        using var dir = new TempDir();
        var file = dir.Write("evidence.bin", "abc");
        var writeTime = File.GetLastWriteTimeUtc(file);
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            EvidenceHasher.HashAsync(dir.Path, HashAlgorithmKind.Sha256, ExaminationContext.Capture(null, null), null, cancel.Token));
        Assert.Equal(writeTime, File.GetLastWriteTimeUtc(file));
    }

    [WindowsJunctionFact]
    public async Task DoesNotFollowADirectoryJunction()
    {
        using var dir = new TempDir();
        var outside = Path.Combine(dir.Path, "outside");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "secret.txt"), "abc");
        var evidence = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "keep.txt"), "abc");
        LinkSupport.CreateJunction(Path.Combine(evidence, "linked-dir"), outside);

        var manifest = await Hash(evidence);
        Assert.Equal(new[] { "keep.txt" }, manifest.Files.Select(file => file.Path).ToArray());
        var skipped = Assert.Single(manifest.Skipped);
        Assert.Equal("linked-dir", skipped.Path);
        Assert.Equal("symbolic link", skipped.Reason);
    }

    [WindowsJunctionFact]
    public async Task RefusesAJunctionAsTheEvidencePath()
    {
        using var dir = new TempDir();
        var target = Path.Combine(dir.Path, "target");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "notes.txt"), "abc");
        var link = Path.Combine(dir.Path, "link-dir");
        LinkSupport.CreateJunction(link, target);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Hash(link));
        Assert.Contains("symbolic link", error.Message, StringComparison.Ordinal);
    }

    private static Task<HashManifest> Hash(string path)
    {
        return EvidenceHasher.HashAsync(
            path,
            HashAlgorithmKind.Sha256,
            ExaminationContext.Capture("A. Examiner", "SYNTH-001"),
            progress: null,
            CancellationToken.None);
    }
}
