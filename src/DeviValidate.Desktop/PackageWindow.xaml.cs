using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using Devi.Theme;
using DeviValidate.Core;
using DeviValidate.Core.IO;
using DeviValidate.Core.Packaging;
using DeviValidate.Core.Reporting;
using Microsoft.Win32;

namespace DeviValidate.Desktop;

public partial class PackageWindow : Window
{
	private sealed record CertificateChoice(string Label, X509Certificate2? Certificate)
	{
		public override string ToString() => Label;
	}

	private readonly VerificationRecord _record;

	private readonly string? _evidencePath;

	private readonly IReadOnlyList<string> _evidencePaths;

	private PackageResult? _result;

	public string? CreatedFolder => _result?.Folder;

	public PackageWindow(VerificationRecord record, string? evidencePath, string suggestedFolder, IReadOnlyList<string>? evidencePaths = null)
	{
		InitializeComponent();
		_record = record;
		_evidencePath = evidencePath;
		_evidencePaths = evidencePaths ?? (string.IsNullOrWhiteSpace(evidencePath) ? Array.Empty<string>() : new string[1] { evidencePath });
		FolderBox.Text = suggestedFolder;
		VerdictText.Text = record.Verdict;
		AddField("Evidence", record.EvidencePath, mono: false);
		AddField("Algorithm", record.Algorithm, mono: false);
		AddField("Verified (UTC)", ValidationPackage.Utc(record.VerifiedAt), mono: false);
		AddField("Verified (local)", ValidationPackage.LocalWithZone(record.VerifiedAt, record.TimeZone), mono: false);
		AddField("Examiner", ReportCopy.OrNotRecorded(record.Examiner), mono: false);
		AddField("Case or reference", ReportCopy.OrNotRecorded(record.CaseReference), mono: false);
		AddField("Validated by", ReportCopy.OrNotRecorded(record.ValidatedBy), mono: false);
		AddField("Lab procedure", ReportCopy.OrNotRecorded(record.LabProcedure), mono: false);
		AddField("Record SHA-256", record.Integrity?.Hash ?? "", mono: true);
		CaseWarning.Visibility = (string.IsNullOrWhiteSpace(record.Examiner) || string.IsNullOrWhiteSpace(record.CaseReference)) ? Visibility.Visible : Visibility.Collapsed;
		var choices = new List<CertificateChoice> { new CertificateChoice("Do not sign (hash-sealed only)", null) };
		foreach (X509Certificate2 cert in ValidationPackage.SigningCertificates())
		{
			string name = cert.GetNameInfo(X509NameType.SimpleName, false);
			string issuer = cert.GetNameInfo(X509NameType.SimpleName, true);
			choices.Add(new CertificateChoice(name + "  (issued by " + issuer + ", expires " + cert.NotAfter.ToString("yyyy-MM-dd") + ")", cert));
		}
		CertificateBox.ItemsSource = choices;
		CertificateBox.SelectedIndex = 0;
		CertificateHint.Text = choices.Count == 1
			? "No signing certificate with a private key was found in your Windows certificate store. Insert your smart card or install your certificate, then reopen this window."
			: (choices.Count - 1) + " signing certificate(s) found in your Windows certificate store. Windows may ask for your PIN when you create the package.";
		CertificateHint.Foreground = DeviTheme.Brush("MutedBrush");
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

	private void AddField(string label, string value, bool mono)
	{
		int row = FieldsGrid.RowDefinitions.Count;
		FieldsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		var l = new TextBlock { Text = label, Style = (Style)FindResource("Text.Small"), Margin = new Thickness(0, 2, 12, 4) };
		var v = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, Style = (Style)FindResource(mono ? "Text.Mono" : "Text.Body"), Margin = new Thickness(0, 1, 0, 4) };
		Grid.SetRow(l, row);
		Grid.SetRow(v, row);
		Grid.SetColumn(v, 1);
		FieldsGrid.Children.Add(l);
		FieldsGrid.Children.Add(v);
	}

	private void ChooseFolder_Click(object sender, RoutedEventArgs e)
	{
		var dialog = new OpenFolderDialog { Title = "Save the validation package in", InitialDirectory = Directory.Exists(FolderBox.Text) ? FolderBox.Text : "" };
		if (dialog.ShowDialog(this) == true)
		{
			FolderBox.Text = dialog.FolderName;
		}
	}

	private void Create_Click(object sender, RoutedEventArgs e)
	{
		var choice = CertificateBox.SelectedItem as CertificateChoice;
		try
		{
			CreateButton.IsEnabled = false;
			SetStatus("Writing the package...", warning: false);
			foreach (string path in _evidencePaths)
			{
				OutputPathGuard.EnsureOutside(path, FolderBox.Text);
			}
			_result = ValidationPackage.Create(_evidencePath, FolderBox.Text, _record, choice?.Certificate);
			CodeText.Text = _result.VerificationCode;
			ResultText.Text = _result.Signed
				? "Signed package written. The PDF carries your signature and the manifest has a CMS signature (package-manifest.json.p7s)."
				: "Hash-sealed package written. It is not signed; package-manifest.json lists the SHA-256 of every file.";
			ResultPath.Text = _result.Folder + Environment.NewLine + _result.ZipPath;
			ResultCard.Visibility = Visibility.Visible;
			ResultCard.UpdateLayout();
			ResultCard.BringIntoView();
			CreateButton.Content = "Create _another";
			SetStatus("Saved outside the evidence location.", warning: false);
		}
		catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException || ex is CryptographicException || ex is ArgumentException)
		{
			CrashLog.Error("Validation package failed", ex);
			SetStatus(ex.Message, warning: true);
		}
		finally
		{
			CreateButton.IsEnabled = true;
		}
	}

	private void SetStatus(string text, bool warning)
	{
		StatusText.Text = text;
		StatusText.Foreground = DeviTheme.Brush(warning ? "WarningBrush" : "SecondaryBrush");
	}

	private void OpenFolder_Click(object sender, RoutedEventArgs e)
	{
		if (_result != null && Directory.Exists(_result.Folder))
		{
			Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = "\"" + _result.Folder + "\"", UseShellExecute = true });
		}
	}

	private void CopyCode_Click(object sender, RoutedEventArgs e)
	{
		if (_result != null)
		{
			try
			{
				Clipboard.SetText(_result.VerificationCode);
				SetStatus("Copied the verification code.", warning: false);
			}
			catch (System.Runtime.InteropServices.COMException)
			{
				SetStatus("The clipboard is busy. Try again.", warning: true);
			}
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
