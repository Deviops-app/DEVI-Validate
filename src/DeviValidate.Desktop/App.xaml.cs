using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DeviValidate.Desktop;

/// <summary>
/// Application entry.
///
/// 1.0.0: the main window is created and shown here, in code. The 0.1.1 sources were rebuilt from
/// the shipped assemblies, and the decompiler cannot recover App.xaml's StartupUri (WPF compiles it
/// into the generated App.g.cs, not into BAML). 0.1.3 therefore started, finished OnStartup, and then
/// sat in the message loop with no window at all. Creating the window explicitly removes that
/// dependency, and every step is written to the log.
///
/// Also in 1.0.0:
/// - One copy per Windows session. A second launch brings the existing window to the front and exits.
/// - A startup watchdog. If no window has been rendered after <see cref="StartupTimeout"/>, the reason
///   is logged and the process exits, so a failed start can never leave a hidden process behind.
/// </summary>
public partial class App : Application
{
	// Same GUID as the installer AppId. "Local\" scopes the names to the current Windows session.
	private const string InstanceKey = "DEVI.Validate.Desktop.7C4E9A2B-6F15-4D8E-9A33-1B6E5D0C4A18";

	private static readonly string MutexName = "Local\\" + InstanceKey + ".Instance";

	private static readonly string ActivateEventName = "Local\\" + InstanceKey + ".Activate";

	private static readonly string AckEventName = "Local\\" + InstanceKey + ".ActivateAck";

	internal static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);

	private static readonly TimeSpan ActivateAckTimeout = TimeSpan.FromSeconds(5);

	private static readonly TimeSpan StaleInstanceMinAge = TimeSpan.FromSeconds(10);

	private readonly DateTime _startedUtc = DateTime.UtcNow;

	private bool _reportedFatal;

	private Mutex? _instanceMutex;

	private bool _ownsInstanceMutex;

	private EventWaitHandle? _activateSignal;

	private EventWaitHandle? _activateAck;

	private Timer? _startupWatchdog;

	private volatile bool _windowShown;

	private volatile bool _fatalDialogOpen;

	private volatile bool _exiting;

	private volatile string _startupStage = "constructor";

	private bool _activateWhenShown;

	public App()
	{
		// Hook everything before XAML loads so a failure in the theme, MainWindow,
		// or window chrome is shown to the examiner instead of the app vanishing.
		AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		CrashLog.Startup();
		// Armed before anything else can hang: a timer thread, so it fires even if the UI thread is stuck.
		_startupWatchdog = new Timer(OnStartupWatchdog, null, StartupTimeout, Timeout.InfiniteTimeSpan);
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		CrashLog.Info("OnStartup | pid " + Environment.ProcessId + " | session " + SafeSessionId() + " | args " + e.Args.Length);
		// Nothing closes the app until the main window exists and owns the lifetime.
		ShutdownMode = ShutdownMode.OnExplicitShutdown;

		Stage("single-instance check");
		if (!TryBecomePrimaryInstance())
		{
			DisarmWatchdog();
			Shutdown(0);
			return;
		}

		Stage("creating main window");
		MainWindow window;
		try
		{
			window = new MainWindow();
		}
		catch (Exception ex)
		{
			FailStartup("Main window constructor failed", ex);
			return;
		}

		MainWindow = window;
		ShutdownMode = ShutdownMode.OnMainWindowClose;
		window.ContentRendered += OnMainWindowRendered;

		Stage("showing main window");
		try
		{
			window.Show();
		}
		catch (Exception ex)
		{
			FailStartup("Main window Show() failed", ex);
			return;
		}

		CrashLog.Info("Main window Show() returned | " + DescribeWindow(window));
		EnsureOnScreen(window);
		try
		{
			window.Activate();
		}
		catch (Exception ex)
		{
			CrashLog.Error("Main window Activate() failed (continuing)", ex);
		}
		Stage("waiting for first render");
		CrashLog.Info("OnStartup complete");
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_exiting = true;
		DisarmWatchdog();
		CrashLog.Info("Exit code " + e.ApplicationExitCode);
		ReleaseInstance();
		base.OnExit(e);
	}

	private void OnMainWindowRendered(object? sender, EventArgs e)
	{
		if (sender is Window window)
		{
			window.ContentRendered -= OnMainWindowRendered;
		}
		_windowShown = true;
		DisarmWatchdog();
		Stage("running");
		CrashLog.Info("Main window shown in " + (int)(DateTime.UtcNow - _startedUtc).TotalMilliseconds + " ms | " + DescribeWindow(MainWindow));
		if (_activateWhenShown)
		{
			_activateWhenShown = false;
			BringMainWindowToFront();
		}
	}

	private void FailStartup(string where, Exception ex)
	{
		CrashLog.Error(where + " (stage: " + _startupStage + ")", ex);
		ShowFatal("DEVI Validate could not open its main window.", ex);
		DisarmWatchdog();
		Shutdown(1);
	}

	private void Stage(string stage)
	{
		_startupStage = stage;
		CrashLog.Info("Startup: " + stage);
	}

	// ---- startup watchdog -------------------------------------------------------------------

	private void OnStartupWatchdog(object? state)
	{
		if (_windowShown || _exiting)
		{
			return;
		}
		if (_fatalDialogOpen)
		{
			// The examiner is reading an error message. Check again later instead of killing it.
			try { _startupWatchdog?.Change(StartupTimeout, Timeout.InfiniteTimeSpan); } catch { }
			return;
		}
		CrashLog.Info("Startup watchdog: no window was shown within " + (int)StartupTimeout.TotalSeconds
			+ " s (last stage: " + _startupStage + "). Closing this process so it does not stay running hidden.");
		try
		{
			// Ask the UI thread first so OnExit runs; if it is stuck, exit from here.
			Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => Shutdown(3)));
			Thread.Sleep(3000);
		}
		catch
		{
		}
		if (!_exiting)
		{
			CrashLog.Info("Startup watchdog: UI thread did not respond; forcing exit.");
			Environment.Exit(3);
		}
	}

	private void DisarmWatchdog()
	{
		Timer? t = Interlocked.Exchange(ref _startupWatchdog, null);
		try { t?.Dispose(); } catch { }
	}

	// ---- single instance --------------------------------------------------------------------

	private bool TryBecomePrimaryInstance()
	{
		try
		{
			_instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
			_ownsInstanceMutex = createdNew || TryTakeMutex(TimeSpan.Zero);
		}
		catch (Exception ex)
		{
			// Never let the single-instance check stop the app from opening.
			CrashLog.Error("Single-instance mutex unavailable (continuing without it)", ex);
			return true;
		}

		if (_ownsInstanceMutex)
		{
			StartActivationListener();
			return true;
		}

		CrashLog.Info("Another DEVI Validate is already running in this session; asking it to come to the front.");
		if (SignalExistingInstance())
		{
			CrashLog.Info("Existing window was brought to the front; this launch exits.");
			return false;
		}

		CrashLog.Info("Existing instance did not answer within " + (int)ActivateAckTimeout.TotalSeconds + " s; closing it and taking over.");
		CloseStaleInstances();
		if (TryTakeMutex(TimeSpan.FromSeconds(5)))
		{
			_ownsInstanceMutex = true;
			StartActivationListener();
			return true;
		}

		CrashLog.Info("Could not take over from the unresponsive instance; this launch exits.");
		ShowFatal("DEVI Validate is already running but is not responding. Close DEVI Validate in Task Manager, then open it again.", null);
		return false;
	}

	private bool TryTakeMutex(TimeSpan timeout)
	{
		try
		{
			return _instanceMutex != null && _instanceMutex.WaitOne(timeout);
		}
		catch (AbandonedMutexException)
		{
			// The previous owner ended without releasing it. We own it now.
			CrashLog.Info("Previous instance ended without releasing the single-instance lock; taking it.");
			return true;
		}
	}

	private void StartActivationListener()
	{
		try
		{
			_activateSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
			_activateAck = new EventWaitHandle(false, EventResetMode.AutoReset, AckEventName);
		}
		catch (Exception ex)
		{
			CrashLog.Error("Activation events unavailable (a second launch cannot bring this window forward)", ex);
			return;
		}
		var thread = new Thread(ActivationLoop) { IsBackground = true, Name = "DEVI single-instance listener" };
		thread.Start();
	}

	private void ActivationLoop()
	{
		while (!_exiting)
		{
			try
			{
				if (_activateSignal == null || !_activateSignal.WaitOne())
				{
					return;
				}
			}
			catch
			{
				return;
			}
			if (_exiting)
			{
				return;
			}
			try
			{
				// Acknowledge only from the UI thread, so a hung UI is detected by the second launch.
				Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
				{
					CrashLog.Info("Second launch detected; bringing the window to the front.");
					if (_windowShown)
					{
						BringMainWindowToFront();
					}
					else
					{
						_activateWhenShown = true;
					}
					try { _activateAck?.Set(); } catch { }
				}));
			}
			catch
			{
			}
		}
	}

	private static bool SignalExistingInstance()
	{
		try
		{
			if (!EventWaitHandle.TryOpenExisting(ActivateEventName, out EventWaitHandle? activate)
				|| !EventWaitHandle.TryOpenExisting(AckEventName, out EventWaitHandle? ack))
			{
				return false;
			}
			using (activate)
			using (ack)
			{
				// This process was started by the user, so it may hand the foreground to the existing one.
				AllowSetForegroundWindow(AsfwAny);
				ack.Reset();
				activate.Set();
				return ack.WaitOne(ActivateAckTimeout);
			}
		}
		catch (Exception ex)
		{
			CrashLog.Error("Could not signal the existing instance", ex);
			return false;
		}
	}

	private static void CloseStaleInstances()
	{
		try
		{
			using Process self = Process.GetCurrentProcess();
			foreach (Process p in Process.GetProcessesByName(self.ProcessName))
			{
				using (p)
				{
					try
					{
						if (p.Id == self.Id || p.SessionId != self.SessionId)
						{
							continue;
						}
						if (p.MainWindowHandle != IntPtr.Zero || DateTime.Now - p.StartTime < StaleInstanceMinAge)
						{
							continue;
						}
						CrashLog.Info("Closing unresponsive windowless DEVI Validate process " + p.Id + " (started " + p.StartTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + ").");
						p.Kill();
						p.WaitForExit(3000);
					}
					catch (Exception ex)
					{
						CrashLog.Error("Could not close process " + p.Id, ex);
					}
				}
			}
		}
		catch (Exception ex)
		{
			CrashLog.Error("Stale-instance cleanup failed", ex);
		}
	}

	private void ReleaseInstance()
	{
		try
		{
			_activateSignal?.Set(); // wake the listener so it sees _exiting
		}
		catch
		{
		}
		try
		{
			if (_ownsInstanceMutex)
			{
				_instanceMutex?.ReleaseMutex();
			}
		}
		catch
		{
		}
		_ownsInstanceMutex = false;
		try { _instanceMutex?.Dispose(); } catch { }
		try { _activateSignal?.Dispose(); } catch { }
		try { _activateAck?.Dispose(); } catch { }
	}

	private void BringMainWindowToFront()
	{
		Window? window = MainWindow;
		if (window == null)
		{
			return;
		}
		try
		{
			if (!window.IsVisible)
			{
				window.Show();
			}
			if (window.WindowState == WindowState.Minimized)
			{
				window.WindowState = WindowState.Normal;
			}
			EnsureOnScreen(window);
			window.Activate();
			// Topmost toggle lifts the window above others without leaving it pinned.
			window.Topmost = true;
			window.Topmost = false;
			window.Focus();
		}
		catch (Exception ex)
		{
			CrashLog.Error("Bring to front failed", ex);
		}
	}

	// ---- window placement / diagnostics -------------------------------------------------------

	/// <summary>Re-centers the window on the primary work area if it ended up outside every screen.</summary>
	private static void EnsureOnScreen(Window window)
	{
		try
		{
			if (window.WindowState != WindowState.Normal)
			{
				return;
			}
			var virtualScreen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
			var bounds = new Rect(window.Left, window.Top, Math.Max(1.0, window.ActualWidth > 0 ? window.ActualWidth : window.Width), Math.Max(1.0, window.ActualHeight > 0 ? window.ActualHeight : window.Height));
			Rect visible = Rect.Intersect(virtualScreen, bounds);
			if (!double.IsNaN(window.Left) && !double.IsNaN(window.Top) && !visible.IsEmpty && visible.Width >= 120 && visible.Height >= 40)
			{
				return;
			}
			CrashLog.Info("Window was off-screen (" + FormatRect(bounds) + ", virtual screen " + FormatRect(virtualScreen) + "); centering it on the work area.");
			Rect work = SystemParameters.WorkArea;
			double w = Math.Min(bounds.Width, work.Width);
			double h = Math.Min(bounds.Height, work.Height);
			window.Width = w;
			window.Height = h;
			window.Left = work.Left + (work.Width - w) / 2.0;
			window.Top = work.Top + (work.Height - h) / 2.0;
		}
		catch (Exception ex)
		{
			CrashLog.Error("Off-screen check failed (continuing)", ex);
		}
	}

	private static string DescribeWindow(Window? window)
	{
		if (window == null)
		{
			return "no window";
		}
		try
		{
			IntPtr hwnd = new WindowInteropHelper(window).Handle;
			return "hwnd 0x" + hwnd.ToString("X")
				+ " | visible " + window.IsVisible
				+ " | " + window.Visibility
				+ " | " + window.WindowState
				+ " | bounds " + FormatRect(new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight))
				+ " | work area " + FormatRect(SystemParameters.WorkArea)
				+ " | virtual screen " + FormatRect(new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight));
		}
		catch (Exception ex)
		{
			return "window state unavailable: " + ex.GetType().Name;
		}
	}

	private static string FormatRect(Rect r)
	{
		if (r.IsEmpty)
		{
			return "empty";
		}
		return string.Format(CultureInfo.InvariantCulture, "{0:0},{1:0} {2:0}x{3:0}", r.Left, r.Top, r.Width, r.Height);
	}

	private static string SafeSessionId()
	{
		try
		{
			using Process self = Process.GetCurrentProcess();
			return self.SessionId.ToString(CultureInfo.InvariantCulture);
		}
		catch
		{
			return "?";
		}
	}

	// ---- error reporting ----------------------------------------------------------------------

	private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		CrashLog.Error("Unhandled UI exception (stage: " + _startupStage + ")", e.Exception);
		bool mainWindowUp = MainWindow is { IsLoaded: true, IsVisible: true };
		e.Handled = true;
		if (mainWindowUp)
		{
			ShowError("DEVI Validate hit an unexpected error and kept running. Your evidence was not changed.", e.Exception, MainWindow);
			return;
		}
		// Startup failed before the main window could be shown. Explain, then close cleanly.
		ShowFatal("DEVI Validate could not open its main window.", e.Exception);
		DisarmWatchdog();
		Shutdown(1);
	}

	private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Exception? exception = e.ExceptionObject as Exception;
		CrashLog.Error("Unhandled exception (terminating=" + e.IsTerminating + ", stage: " + _startupStage + ")", exception);
		if (e.IsTerminating)
		{
			ShowFatal("DEVI Validate has to close because of an unexpected error.", exception);
		}
	}

	private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		CrashLog.Error("Unobserved task exception", e.Exception);
		e.SetObserved();
	}

	private void ShowFatal(string headline, Exception? exception)
	{
		if (_reportedFatal)
		{
			return;
		}
		_reportedFatal = true;
		_fatalDialogOpen = true;
		try
		{
			ShowError(headline, exception, null);
		}
		finally
		{
			_fatalDialogOpen = false;
		}
	}

	internal static void ShowError(string headline, Exception? exception, Window? owner)
	{
		string text = headline
			+ Environment.NewLine + Environment.NewLine
			+ (exception != null ? exception.GetType().Name + ": " + exception.Message + Environment.NewLine + Environment.NewLine : "")
			+ "Details were saved to:" + Environment.NewLine + CrashLog.FilePath
			+ Environment.NewLine + Environment.NewLine
			+ "Send that file to contact@deviops.app so we can fix it.";
		try
		{
			if (owner != null)
			{
				MessageBox.Show(owner, text, "DEVI Validate", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else
			{
				MessageBox.Show(text, "DEVI Validate", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		catch (Exception boxFailure)
		{
			CrashLog.Error("Could not show error dialog", boxFailure);
		}
	}

	private const int AsfwAny = -1;

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AllowSetForegroundWindow(int dwProcessId);
}
