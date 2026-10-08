using System.Diagnostics;

namespace DeviValidate.Core.Tests;

internal sealed class SymlinkFactAttribute : FactAttribute
{
    public SymlinkFactAttribute()
    {
        var reason = LinkSupport.SymlinkUnavailableReason();
        if (reason is not null)
        {
            Skip = reason;
        }
    }
}

internal sealed class WindowsJunctionFactAttribute : FactAttribute
{
    public WindowsJunctionFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Directory junctions are a Windows reparse point. This check runs in the Windows CI job and was skipped, not passed.";
        }
    }
}

internal static class LinkSupport
{
    public static string? SymlinkUnavailableReason()
    {
        var dir = Path.Combine(Path.GetTempPath(), "devi-link-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var target = Path.Combine(dir, "target.txt");
            File.WriteAllText(target, "x");
            File.CreateSymbolicLink(Path.Combine(dir, "link.txt"), target);
            return null;
        }
        catch (IOException ex) when (LacksLinkPrivilege(ex))
        {
            return "Symbolic link creation is not permitted for this account ("
                + ex.Message.Trim()
                + "). The check was skipped, not passed. On Windows, Developer Mode or an administrator account can create symbolic links. Directory junction checks still run without that privilege.";
        }
        finally
        {
            TestFiles.DeleteTree(dir);
        }
    }

    public static void CreateFileSymlink(string link, string target)
    {
        File.CreateSymbolicLink(link, target);
    }

    public static void CreateDirectorySymlink(string link, string target)
    {
        Directory.CreateSymbolicLink(link, target);
    }

    public static void CreateJunction(string junction, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException(
                "Directory junctions are a Windows reparse point. This check runs in the Windows CI job and was skipped, not passed.");
        }

        if (!Directory.Exists(target))
        {
            throw new DirectoryNotFoundException(target);
        }

        var command = "mklink /J \"" + junction + "\" \"" + target + "\"";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };
        if (!process.Start())
        {
            throw new InvalidOperationException("Could not start cmd.exe to create a directory junction.");
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 || !Directory.Exists(junction))
        {
            throw new InvalidOperationException(
                "mklink /J failed with exit code " + process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ". " + stdout + " " + stderr);
        }
    }

    private static bool LacksLinkPrivilege(IOException ex)
    {
        return ex.Message.Contains("privilege", StringComparison.OrdinalIgnoreCase);
    }
}
