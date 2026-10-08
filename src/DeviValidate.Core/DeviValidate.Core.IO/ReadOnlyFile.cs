using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DeviValidate.Core.IO;

/// <summary>
/// Opens evidence for reading only. The caller never receives a writable stream.
/// On Linux the open requests O_NOATIME so a successful open does not update the access time.
/// If the kernel refuses that flag, the file is opened read-only without it.
/// The tool never sets access or write timestamps itself.
/// </summary>
public static class ReadOnlyFile
{
	private static class Native
	{
		[DllImport("libc", SetLastError = true)]
		internal static extern int open(string pathname, int flags);
	}

	private const int O_RDONLY = 0;

	private const int O_NOATIME = 262144;

	private const int O_CLOEXEC = 524288;

	public static FileStream Open(string path)
	{
		if (OperatingSystem.IsLinux())
		{
			FileStream fileStream = TryOpenLinux(path);
			if (fileStream != null)
			{
				return fileStream;
			}
		}
		return new FileStream(path, new FileStreamOptions
		{
			Mode = FileMode.Open,
			Access = FileAccess.Read,
			Share = FileShare.Read,
			Options = (FileOptions.Asynchronous | FileOptions.SequentialScan),
			BufferSize = 1048576
		});
	}

	private static FileStream? TryOpenLinux(string path)
	{
		int num = Native.open(path, 786432);
		if (num < 0)
		{
			num = Native.open(path, 524288);
		}
		if (num < 0)
		{
			return null;
		}
		SafeFileHandle safeFileHandle = new SafeFileHandle(num, ownsHandle: true);
		try
		{
			return new FileStream(safeFileHandle, FileAccess.Read, 1048576, isAsync: false);
		}
		catch
		{
			safeFileHandle.Dispose();
			throw;
		}
	}
}
