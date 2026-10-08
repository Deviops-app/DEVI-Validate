using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Devi.Theme;
using DeviValidate.Core.Packaging;
using Microsoft.Win32;

namespace DeviValidate.Desktop;

public partial class VerifyPackageWindow : Window
{
	public sealed class CheckRow
	{
		public string Outcome { get; init; } = "";
		public string Name { get; init; } = "";
		public string Detail { get; init; } = "";
		public Brush PillBackground { get; init; } = DeviTheme.Brush("ClearBrush");
		public Brush PillForeground { get; init; } = DeviTheme.Brush("ClearBrush");
	}

	public VerifyPackageWindow(string? initialPath = null)
	{
		InitializeComponent();
		if (!string.IsNullOrWhiteSpace(initialPath))
		{
			PathBox.Text = initialPath;
		}
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		try
		{
			DeviWindowChrome.Attach(this);
		}
		catch (Exception ex)
		{
			CrashLog.Error("Window chrome attach failed (continuing)", ex);
		}
	}

	private void ChooseFolder_Click(object sender, RoutedEventArgs e)
	{
		var dialog = new OpenFolderDialog { Title = "Choose the package folder" };
		if (dialog.ShowDialog(this) == true)
		{
			PathBox.Text = dialog.FolderName;
		}
	}

	private void ChooseZip_Click(object sender, RoutedEventArgs e)
	{
		var dialog = new OpenFileDialog { Title = "Choose the package .zip or manifest", Filter = "Validation package (*.zip;package-manifest.json)|*.zip;package-manifest.json|All files (*.*)|*.*", CheckFileExists = true };
		if (dialog.ShowDialog(this) == true)
		{
			PathBox.Text = dialog.FileName;
		}
	}

	private void Window_DragOver(object sender, DragEventArgs e)
	{
		e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		e.Handled = true;
	}

	private void Window_Drop(object sender, DragEventArgs e)
	{
		if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
		{
			PathBox.Text = paths[0];
			Verify();
		}
	}

	private void Verify_Click(object sender, RoutedEventArgs e) => Verify();

	private void Verify()
	{
		string path = PathBox.Text.Trim().Trim('"');
		if (path.Length == 0)
		{
			StatusBar.LeftText = "Choose a package folder, .zip, or package-manifest.json.";
			return;
		}
		PackageVerification result = PackageVerifier.Verify(path, CodeBox.Text);
		ChecksList.ItemsSource = result.Checks.Select(c => new CheckRow
		{
			Outcome = c.Outcome switch { CheckOutcome.Pass => "Pass", CheckOutcome.Fail => "Fail", CheckOutcome.Warning => "Review", _ => "Note" },
			Name = c.Name,
			Detail = c.Detail,
			PillBackground = DeviTheme.Brush(c.Outcome switch { CheckOutcome.Pass => "ChipMatchBrush", CheckOutcome.Fail => "ChipMismatchBrush", CheckOutcome.Warning => "ChipMissingBrush", _ => "NeutralSoftBrush" }),
			PillForeground = DeviTheme.Brush(c.Outcome switch { CheckOutcome.Pass => "SuccessBrush", CheckOutcome.Fail => "DangerBrush", CheckOutcome.Warning => "WarningBrush", _ => "SecondaryBrush" })
		}).ToList();
		EmptyHint.Visibility = Visibility.Collapsed;
		VerdictBand.Visibility = Visibility.Visible;
		VerdictBand.Background = DeviTheme.Brush(result.Passed ? "SuccessSoftBrush" : "DangerSoftBrush");
		VerdictBand.BorderBrush = DeviTheme.Brush(result.Passed ? "SuccessBrush" : "DangerBrush");
		VerdictKicker.Text = result.Passed ? "PACKAGE VERIFIED" : "PACKAGE DID NOT VERIFY";
		VerdictKicker.Foreground = DeviTheme.Brush(result.Passed ? "SuccessBrush" : "DangerBrush");
		VerdictText.Text = result.Manifest?.VerificationCode ?? "No package manifest";
		VerdictDetail.Text = result.Summary + (result.Manifest == null ? "" : "  Case: " + (result.Manifest.CaseReference ?? "not recorded") + ". Examiner: " + (result.Manifest.Examiner ?? "not recorded") + ". " + result.Manifest.Verdict);
		StatusBar.LeftText = "Checked " + result.Source + " offline.";
	}
}
