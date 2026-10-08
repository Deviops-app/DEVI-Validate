namespace DeviValidate.Core.Tests;

internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = Directory.CreateTempSubdirectory("devi-validate-").FullName;
    }

    public string Path { get; }

    public string Write(string relative, string content)
    {
        var full = System.IO.Path.Combine(Path, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        var parent = System.IO.Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        File.WriteAllText(full, content);
        return full;
    }

    public string WriteBytes(string relative, byte[] content)
    {
        var full = Write(relative, "");
        File.WriteAllBytes(full, content);
        return full;
    }

    public void Dispose()
    {
        TestFiles.DeleteTree(Path);
    }
}

internal static class TestFiles
{
    public static void DeleteTree(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                RemoveReparsePoints(root);
                ClearReadOnly(root);
                Directory.Delete(root, recursive: true);
            }
            else if (File.Exists(root))
            {
                ClearFileAttributes(root);
                File.Delete(root);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void RemoveReparsePoints(string directory)
    {
        foreach (var entry in Entries(directory))
        {
            if (!TryGetAttributes(entry, out var attributes))
            {
                continue;
            }

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                DeleteReparsePoint(entry, attributes);
                continue;
            }

            if ((attributes & FileAttributes.Directory) != 0)
            {
                RemoveReparsePoints(entry);
            }
        }
    }

    private static void DeleteReparsePoint(string entry, FileAttributes attributes)
    {
        try
        {
            File.SetAttributes(entry, FileAttributes.Normal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        try
        {
            if ((attributes & FileAttributes.Directory) != 0)
            {
                Directory.Delete(entry);
            }
            else
            {
                File.Delete(entry);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void ClearReadOnly(string directory)
    {
        foreach (var entry in Entries(directory))
        {
            if (!TryGetAttributes(entry, out var attributes))
            {
                continue;
            }

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                DeleteReparsePoint(entry, attributes);
                continue;
            }

            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                ClearFileAttributes(entry);
            }

            if ((attributes & FileAttributes.Directory) != 0)
            {
                ClearReadOnly(entry);
            }
        }
    }

    private static void ClearFileAttributes(string entry)
    {
        try
        {
            File.SetAttributes(entry, FileAttributes.Normal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool TryGetAttributes(string entry, out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(entry);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            attributes = default;
            return false;
        }
    }

    private static string[] Entries(string directory)
    {
        try
        {
            return Directory.EnumerateFileSystemEntries(directory).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }
}

internal sealed class SyncProgress<T> : IProgress<T>
{
    private readonly Action<T> _report;

    public SyncProgress(Action<T> report)
    {
        _report = report;
    }

    public void Report(T value) => _report(value);
}
