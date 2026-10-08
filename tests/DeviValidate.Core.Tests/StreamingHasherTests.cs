using System.Text;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Tests;

public class StreamingHasherTests
{
    [Theory]
    [InlineData("", HashAlgorithmKind.Sha256, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("abc", HashAlgorithmKind.Sha256, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", HashAlgorithmKind.Sha256, "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1")]
    [InlineData("", HashAlgorithmKind.Sha1, "da39a3ee5e6b4b0d3255bfef95601890afd80709")]
    [InlineData("abc", HashAlgorithmKind.Sha1, "a9993e364706816aba3e25717850c26c9cd0d89d")]
    [InlineData("", HashAlgorithmKind.Md5, "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("abc", HashAlgorithmKind.Md5, "900150983cd24fb0d6963f7d28e17f72")]
    public async Task HashesKnownVectorsAcrossTinyReads(string text, HashAlgorithmKind algorithm, string expected)
    {
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        var hash = await StreamingHasher.HashAsync(stream, algorithm, progress: null, CancellationToken.None, bufferSize: 1);
        Assert.Equal(expected, hash);
    }

    [Fact]
    public async Task ReportsProgressAndHonorsCancellation()
    {
        var data = new byte[64];
        Random.Shared.NextBytes(data);
        await using var stream = new MemoryStream(data);
        var seen = new List<long>();
        using var cancel = new CancellationTokenSource();
        var progress = new SyncProgress<long>(read =>
        {
            seen.Add(read);
            if (read >= 8)
            {
                cancel.Cancel();
            }
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            StreamingHasher.HashAsync(stream, HashAlgorithmKind.Sha256, progress, cancel.Token, bufferSize: 4));
        Assert.Contains(seen, value => value >= 8);
        Assert.DoesNotContain(seen, value => value == data.Length);
    }
}
