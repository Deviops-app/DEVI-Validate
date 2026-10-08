using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using DeviValidate.Core;
using Devi.Updates;

namespace DeviValidate.Desktop;

public partial class UpdateWindow : Window
{
	private readonly UpdateCheckResult _result;

	private readonly bool _loading = true;

	private CancellationTokenSource? _download;

	private string? _savedPath;

	public UpdateWindow(UpdateCheckResult result)
	{
		InitializeComponent();
		_result = result;
		CurrentVersionText.Text = DeviValidate.Core.ToolInfo.Version;
		StatusText.Text = result.Message;
		bool flag = result.Status == UpdateStatus.UpdateAvailable;
		switch (result.Status)
		{
		case UpdateStatus.UpdateAvailable:
			KickerText.Text = "Update available";
			KickerText.Foreground = (Brush)FindResource("WarningBrush");
			KickerBand.BorderBrush = (Brush)FindResource("WarningBrush");
			break;
		case UpdateStatus.UpToDate:
			KickerText.Text = "Up to date";
			KickerText.Foreground = (Brush)FindResource("SuccessBrush");
			KickerBand.BorderBrush = (Brush)FindResource("SuccessBrush");
			break;
		default:
			KickerText.Text = "This copy is newer";
			KickerText.Foreground = (Brush)FindResource("AccentBrush");
			KickerBand.BorderBrush = (Brush)FindResource("AccentBrush");
			break;
		}
		UpdateProduct product = result.Product;
		if (product != null)
		{
			RemoteVersionText.Text = product.Version;
			ReleasedText.Text = (string.IsNullOrWhiteSpace(product.Released) ? "" : ("Released " + product.Released));
			if (flag && product.Notes.Length > 0)
			{
				NotesText.Text = product.Notes;
				NotesText.Visibility = Visibility.Visible;
			}
			ShowFileCard(InstallerCard, InstallerNameText, InstallerHashText, DownloadInstallerButton, product.Installer, flag);
			ShowFileCard(PortableCard, PortableNameText, PortableHashText, DownloadPortableButton, product.Portable, flag);
		}
		else
		{
			RemoteVersionText.Text = "Unknown";
		}
		CheckOnLaunch.IsChecked = ValidateSettings.Load().CheckOnLaunch;
		_loading = false;
	}

	private static void ShowFileCard(FrameworkElement card, TextBlock name, TextBlock hash, Button download, UpdateFile? file, bool offer)
	{
		if (file == null)
		{
			card.Visibility = Visibility.Collapsed;
			return;
		}
		card.Visibility = Visibility.Visible;
		name.Text = file.Name;
		hash.Text = file.Sha256;
		download.Visibility = ((!offer) ? Visibility.Collapsed : Visibility.Visible);
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		Devi.Theme.DeviWindowChrome.Attach(this);
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		_download?.Cancel();
		base.OnClosing(e);
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void DownloadInstaller_Click(object sender, RoutedEventArgs e)
	{
		UpdateFile updateFile = _result.Product?.Installer;
		if (updateFile != null)
		{
			await DownloadAsync(updateFile);
		}
	}

	private async void DownloadPortable_Click(object sender, RoutedEventArgs e)
	{
		UpdateFile updateFile = _result.Product?.Portable;
		if (updateFile != null)
		{
			await DownloadAsync(updateFile);
		}
	}

	private void StartInstaller_Click(object sender, RoutedEventArgs e)
	{
		if (_savedPath == null)
		{
			return;
		}
		try
		{
			UpdateApply.StartInstaller(_savedPath);
			ProgressText.Text = "The installer is open. It was not started with a silent flag. Close DEVI Validate if it asks to replace files that are in use.";
		}
		catch (Exception ex) when (((ex is IOException || ex is Win32Exception || ex is InvalidOperationException) ? 1 : 0) != 0)
		{
			ProgressText.Text = ex.Message;
		}
	}

	private void Extract_Click(object sender, RoutedEventArgs e)
	{
		if (_savedPath == null || _result.Product == null)
		{
			return;
		}
		string text;
		try
		{
			text = UpdateApply.PlanPortableDirectory("DEVI-Validate", _result.Product.Version, System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "DEVI", "Validate", "updates"));
		}
		catch (IOException ex)
		{
			ProgressText.Text = ex.Message;
			return;
		}
		if (MessageBox.Show(this, "The new copy will be unpacked to:\n" + text + "\n\nThe current folder is left in place. DEVI Validate will close so the files are not in use.", "Extract update", MessageBoxButton.OKCancel, MessageBoxImage.Asterisk) == MessageBoxResult.OK)
		{
			try
			{
				UpdateApply.ExtractAfterExit(_savedPath, text, "devi-validate");
			}
			catch (Exception ex2) when (((ex2 is IOException || ex2 is Win32Exception || ex2 is InvalidOperationException) ? 1 : 0) != 0)
			{
				ProgressText.Text = ex2.Message;
				return;
			}
			Application.Current.Shutdown();
		}
	}

	private void ShowFile_Click(object sender, RoutedEventArgs e)
	{
		if (_savedPath == null)
		{
			return;
		}
		try
		{
			UpdateApply.ShowFile(_savedPath);
		}
		catch (Exception ex) when (((ex is IOException || ex is Win32Exception || ex is InvalidOperationException) ? 1 : 0) != 0)
		{
			ProgressText.Text = ex.Message;
		}
	}

	private void CancelDownload_Click(object sender, RoutedEventArgs e)
	{
		_download?.Cancel();
	}

	private void CheckOnLaunch_Changed(object sender, RoutedEventArgs e)
	{
		if (_loading)
		{
			return;
		}
		try
		{
			ValidateSettings updateSettings = ValidateSettings.Load();
			updateSettings.CheckOnLaunch = CheckOnLaunch.IsChecked == true;
			updateSettings.Save();
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			ProgressText.Text = ex.Message;
		}
	}

	private async Task DownloadAsync(UpdateFile file)
	{
		SetBusy(busy: true);
		_download = new CancellationTokenSource();
		ProgressText.Text = "Downloading " + file.Name;
		ReadyText.Visibility = Visibility.Collapsed;
		StartInstallerButton.Visibility = Visibility.Collapsed;
		ExtractButton.Visibility = Visibility.Collapsed;
		ShowFileButton.Visibility = Visibility.Collapsed;
		try
		{
			using HttpClient http = UpdateClient.CreateFeedClient("DEVI-Validate", ToolInfo.Version);
			string directory = Path.Combine(Path.GetTempPath(), "DEVI", "Validate", "downloads");
			Progress<long> progress = new Progress<long>(delegate(long bytes)
			{
				ProgressText.Text = "Downloaded " + DeviValidate.Core.ByteSize.Format(bytes);
			});
			string text = (_savedPath = await UpdateClient.DownloadVerifiedAsync(http, file, directory, progress, _download.Token));
			ProgressText.Text = "Saved " + text;
			ReadyText.Text = "The SHA-256 matches the signed feed. The installer and the app are Authenticode-signed. Windows SmartScreen may still warn for a newly signed release. Nothing starts until you press a button below.";
			ReadyText.Visibility = Visibility.Visible;
			ShowFileButton.Visibility = Visibility.Visible;
			if (file.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				StartInstallerButton.Visibility = Visibility.Visible;
			}
			if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			{
				ExtractButton.Visibility = Visibility.Visible;
			}
		}
		catch (OperationCanceledException)
		{
			ProgressText.Text = "Download canceled. The partial file was not kept.";
		}
		catch (Exception ex2) when (((ex2 is UpdateException || ex2 is IOException || ex2 is HttpRequestException || ex2 is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			ProgressText.Text = ex2.Message;
		}
		finally
		{
			_download.Dispose();
			_download = null;
			SetBusy(busy: false);
		}
	}

	private void SetBusy(bool busy)
	{
		DownloadInstallerButton.IsEnabled = !busy;
		DownloadPortableButton.IsEnabled = !busy;
		CancelDownloadButton.Visibility = ((!busy) ? Visibility.Collapsed : Visibility.Visible);
	}

}
