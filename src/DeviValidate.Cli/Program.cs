using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DeviValidate.Core.Expected.Vendor;
using Devi.Updates;

namespace DeviValidate.Cli;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		CancellationTokenSource cancel = new CancellationTokenSource();
		try
		{
			Console.CancelKeyPress += delegate(object? _, ConsoleCancelEventArgs eventArgs)
			{
				eventArgs.Cancel = true;
				try
				{
					cancel.Cancel();
				}
				catch (ObjectDisposedException)
				{
				}
			};
			try
			{
				VendorParserRegistry.RegisterBuiltIns();
				int code = await CliApp.RunAsync(args, cancel.Token).ConfigureAwait(continueOnCapturedContext: false);
				PauseIfDoubleClicked(args);
				return code;
			}
			catch (OperationCanceledException)
			{
				Console.Error.WriteLine();
				Console.Error.WriteLine("Canceled.");
				return 130;
			}
			catch (Exception ex2) when (IsUsageError(ex2))
			{
				Console.Error.WriteLine(ex2.Message);
				return 1;
			}
		}
		finally
		{
			if (cancel != null)
			{
				((IDisposable)cancel).Dispose();
			}
		}
	}

	/// <summary>
	/// When someone double-clicks devi-validate.exe in Explorer, Windows opens a console just for it
	/// and closes it the moment the help text prints, which looks like a crash. Keep that window open.
	/// Never pauses when arguments are given or when run from an existing terminal or script.
	/// </summary>
	private static void PauseIfDoubleClicked(string[] args)
	{
		try
		{
			if (args.Length != 0 || !OperatingSystem.IsWindows() || Console.IsInputRedirected || Console.IsOutputRedirected)
			{
				return;
			}
			uint[] buffer = new uint[4];
			if (GetConsoleProcessList(buffer, (uint)buffer.Length) != 1)
			{
				return;
			}
			Console.WriteLine();
			Console.WriteLine("This is the DEVI Validate command-line tool. To open the app, use the DEVI Validate shortcut or app\\DEVI-Validate.exe.");
			Console.Write("Press Enter to close this window.");
			Console.ReadLine();
		}
		catch
		{
		}
	}

	[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
	private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

	private static bool IsUsageError(Exception ex)
	{
		if (ex is InvalidDataException || ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is CliException || ex is UpdateException)
		{
			return true;
		}
		return false;
	}
}
