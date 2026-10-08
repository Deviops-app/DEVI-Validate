using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using DeviValidate.Core;
using DeviValidate.Core.Expected;
using ByteSize = DeviValidate.Core.ByteSize;
using DeviValidate.Core.Expected.Vendor;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.IO;
using DeviValidate.Core.Reporting;
using DeviValidate.Core.SelfTest;
using Devi.Updates;
using Devi.Theme.Profile;
using Microsoft.Win32;

namespace DeviValidate.Desktop;

public partial class MainWindow : Window
{
	private readonly ObservableCollection<ResultRow> _rows = new ObservableCollection<ResultRow>();

	private ICollectionView? _view;

	private string _statusFilter = "All";

	private bool _busy;

	private bool _evidenceHot;

	private bool _expectedHot;

	private CancellationTokenSource? _cancel;

	private HashManifest? _manifest;

	private VerificationRecord? _verification;

	private SelfTestRecord? _selfTest;

	private readonly PathSelection _evidence = new PathSelection();

	private readonly ExpectedHashInput _expectedInput = new ExpectedHashInput();

	private bool _syncingExpected;

	private string? _lastExportFolder;

	private string? _lastExportFile;

	/// <summary>True after the user tried to hash without the required case details, so field messages show.</summary>
	private bool _showCaseErrors;

	/// <summary>The lab procedure value this window filled in from the expected file, so it can be replaced but a typed value is kept.</summary>
	private string? _autoLabProcedure;

	private const string CaseHint = "Add the examiner and case number in Case details to continue.";

	public MainWindow()
	{
		InitializeComponent();
		_view = CollectionViewSource.GetDefaultView(_rows);
		_view.Filter = FilterRow;
		ResultsGrid.ItemsSource = _view;
		TitleBar.Detail = "v" + ToolInfo.Version;
		StatusBar.RightText = "DEVI Validate " + ToolInfo.Version;
		VendorParserRegistry.RegisterBuiltIns();
		foreach (string source in ExpectedHashSources.All)
		{
			LabProcedureBox.Items.Add(source);
		}
		ApplyProfileDefaults(includeAlgorithm: true);
		UpdateCaseState();
		base.Loaded += OnLoadedCheckForUpdates;
	}

	/// <summary>Fills Case details from the shared profile and the Validate defaults. Never fills the case number.</summary>
	private void ApplyProfileDefaults(bool includeAlgorithm)
	{
		ExaminerProfile profile = ExaminerProfile.Load();
		ValidateSettings settings = ValidateSettings.Load();
		ExaminerBox.Text = profile.ExaminerName ?? "";
		AgencyBox.Text = profile.Agency ?? "";
		CaseBox.Text = "";
		ValidatedByBox.Text = settings.DefaultValidatedBy ?? "";
		LabProcedureBox.SelectedIndex = -1;
		LabProcedureBox.Text = settings.DefaultLabProcedure ?? "";
		_autoLabProcedure = null;
		ValidationDateBox.SelectedDate = DateTime.Today;
		if (includeAlgorithm && !string.IsNullOrEmpty(settings.DefaultAlgorithm))
		{
			foreach (ComboBoxItem item in AlgorithmBox.Items.OfType<ComboBoxItem>())
			{
				if (string.Equals(item.Content as string, settings.DefaultAlgorithm, StringComparison.Ordinal))
				{
					AlgorithmBox.SelectedItem = item;
				}
			}
		}
	}

	private bool CaseReady => !string.IsNullOrWhiteSpace(ExaminerBox.Text) && !string.IsNullOrWhiteSpace(CaseBox.Text);

	/// <summary>Enables Verify and Hash only when the required case details are present, and shows the hint and field messages.</summary>
	private void UpdateCaseState()
	{
		bool examinerMissing = string.IsNullOrWhiteSpace(ExaminerBox.Text);
		bool caseMissing = string.IsNullOrWhiteSpace(CaseBox.Text);
		bool ready = !examinerMissing && !caseMissing;
		VerifyButton.IsEnabled = !_busy && ready;
		HashButton.IsEnabled = !_busy && ready;
		VerifyButton.ToolTip = ready ? "Hash the evidence and compare it with the expected hash from step 3." : CaseHint;
		HashButton.ToolTip = ready ? "Compute a new hash without comparing it. This is not a verification." : CaseHint;
		CaseHintPanel.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
		CaseHintText.Text = examinerMissing && caseMissing ? CaseHint : (examinerMissing ? "Add the examiner in Case details to continue." : "Add the case number in Case details to continue.");
		ShowFieldError(ExaminerBox, ExaminerError, _showCaseErrors && examinerMissing);
		ShowFieldError(CaseBox, CaseError, _showCaseErrors && caseMissing);
		string summary = ready ? ExaminerBox.Text.Trim() + "  |  " + CaseBox.Text.Trim() : (examinerMissing && caseMissing ? "Examiner and case number are required" : (examinerMissing ? "Examiner is required" : "Case number is required"));
		CaseSummary.Text = summary;
		CaseSummary.Foreground = (Brush)FindResource(ready ? "MutedBrush" : "WarningBrush");
	}

	private void ShowFieldError(TextBox box, TextBlock message, bool show)
	{
		message.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
		if (show)
		{
			box.BorderBrush = (Brush)FindResource("DangerBrush");
		}
		else
		{
			box.ClearValue(Control.BorderBrushProperty);
		}
	}

	/// <summary>Opens Case details, shows what is missing, and puts the cursor in the first missing field.</summary>
	private void ShowCaseRequired()
	{
		_showCaseErrors = true;
		CaseExpander.IsExpanded = true;
		UpdateCaseState();
		CaseExpander.BringIntoView();
		TextBox target = string.IsNullOrWhiteSpace(ExaminerBox.Text) ? ExaminerBox : CaseBox;
		Dispatcher.BeginInvoke(new Action(() => target.Focus()), System.Windows.Threading.DispatcherPriority.Input);
	}

	private void CaseField_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (IsLoaded)
		{
			UpdateCaseState();
		}
	}

	private void CaseField_LostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox box && string.IsNullOrWhiteSpace(box.Text) && !CaseReady && box.IsLoaded)
		{
			if (box == ExaminerBox)
			{
				ShowFieldError(ExaminerBox, ExaminerError, show: true);
			}
			else if (box == CaseBox)
			{
				ShowFieldError(CaseBox, CaseError, show: true);
			}
		}
	}

	private void GoToCaseDetails_Click(object sender, RoutedEventArgs e)
	{
		ShowCaseRequired();
	}

	/// <summary>A disabled button gets no click. The row around it does, so a press on Verify or Hash only points to Case details.</summary>
	private void ActionArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (_busy || CaseReady)
		{
			return;
		}
		foreach (Button button in new[] { VerifyButton, HashButton })
		{
			Point point = e.GetPosition(button);
			if (point.X >= 0.0 && point.Y >= 0.0 && point.X <= button.ActualWidth && point.Y <= button.ActualHeight)
			{
				ShowCaseRequired();
				e.Handled = true;
				return;
			}
		}
	}

	private void LabProcedure_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (LabProcedureBox.SelectedItem as string == ExpectedHashSources.Other)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				LabProcedureBox.SelectedIndex = -1;
				LabProcedureBox.Text = "";
				_autoLabProcedure = null;
				LabProcedureBox.Focus();
			}));
		}
	}

	private string LabProcedureValue()
	{
		string text = (LabProcedureBox.Text ?? "").Trim();
		return text == ExpectedHashSources.Other ? "" : text;
	}

	private void More_Click(object sender, RoutedEventArgs e)
	{
		MoreMenu.PlacementTarget = MoreButton;
		MoreMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
		MoreMenu.IsOpen = true;
	}

	private async void Settings_Click(object sender, RoutedEventArgs e)
	{
		await OpenSettingsAsync();
	}

	private async Task OpenSettingsAsync()
	{
		SettingsWindow window = new SettingsWindow { Owner = this };
		window.ShowDialog();
		if (window.Saved && !_busy)
		{
			// Fill only empty fields, so nothing typed for this run is replaced.
			ExaminerProfile profile = ExaminerProfile.Load();
			ValidateSettings settings = ValidateSettings.Load();
			FillIfEmpty(ExaminerBox, profile.ExaminerName);
			FillIfEmpty(AgencyBox, profile.Agency);
			FillIfEmpty(ValidatedByBox, settings.DefaultValidatedBy);
			if (string.IsNullOrWhiteSpace(LabProcedureBox.Text) && !string.IsNullOrWhiteSpace(settings.DefaultLabProcedure))
			{
				LabProcedureBox.Text = settings.DefaultLabProcedure;
			}
			UpdateCaseState();
		}
		if (window.RequestedUpdateCheck)
		{
			await CheckForUpdatesAsync();
		}
	}

	private static void FillIfEmpty(TextBox box, string? value)
	{
		if (string.IsNullOrWhiteSpace(box.Text) && !string.IsNullOrWhiteSpace(value))
		{
			box.Text = value;
		}
	}

	protected override async void OnPreviewKeyDown(KeyEventArgs e)
	{
		base.OnPreviewKeyDown(e);
		if (e.Key == Key.F1 && Keyboard.Modifiers == ModifierKeys.None)
		{
			e.Handled = true;
			HowTo_Click(this, new RoutedEventArgs());
		}
		else if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
		{
			e.Handled = true;
			await OpenSettingsAsync();
		}
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		// Cosmetic only (dark title bar, maximize bounds, centering). Never fatal.
		try
		{
			Devi.Theme.DeviWindowChrome.Attach(this);
		}
		catch (Exception ex)
		{
			CrashLog.Error("Window chrome attach failed (continuing)", ex);
		}
		try
		{
			Devi.Theme.DeviWindowChrome.FitToWorkArea(this, 1200.0, 860.0);
		}
		catch (Exception ex)
		{
			CrashLog.Error("Fit to work area failed (continuing)", ex);
		}
	}

	private void MainScroll_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (MainScroll.ViewportHeight > 1.0)
		{
			MainContent.MinHeight = MainScroll.ViewportHeight;
		}
	}

	private async void Hash_Click(object sender, RoutedEventArgs e)
	{
		await RunAsync(verify: false);
	}

	private async void Verify_Click(object sender, RoutedEventArgs e)
	{
		await RunAsync(verify: true);
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		_cancel?.Cancel();
		SetStatus("Canceling.");
	}

	private async void About_Click(object sender, RoutedEventArgs e)
	{
		AboutWindow aboutWindow = new AboutWindow();
		aboutWindow.Owner = this;
		aboutWindow.ShowDialog();
		if (aboutWindow.RequestedUpdateCheck)
		{
			await CheckForUpdatesAsync();
		}
	}

	private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
	{
		await CheckForUpdatesAsync();
	}

	private async void OnLoadedCheckForUpdates(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoadedCheckForUpdates;
		// Optional check on open. It must never close or block the app:
		// every failure is logged and ignored, and hashing works offline.
		try
		{
			if (!ValidateSettings.Load().CheckOnLaunch || _busy)
			{
				return;
			}
			// Let the window finish its first layout before touching the network.
			await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
			using HttpClient http = UpdateClient.CreateFeedClient("DEVI-Validate", ToolInfo.Version);
			using ECDsa key = UpdateTrust.CreatePublicKey();
			UpdateCheckResult updateCheckResult = await UpdateClient.CheckConfiguredAsync(http, "devi-validate", Version.Parse(ToolInfo.Version), key, CancellationToken.None);
			if (updateCheckResult.Status != UpdateStatus.UpdateAvailable || _busy)
			{
				return;
			}
			UpdateWindow updateWindow = new UpdateWindow(updateCheckResult);
			updateWindow.Owner = this;
			updateWindow.ShowDialog();
		}
		catch (Exception ex)
		{
			CrashLog.Error("Update check on open failed (ignored)", ex);
		}
	}

	private async Task CheckForUpdatesAsync()
	{
		if (_busy)
		{
			SetStatus("Finish the current hash before checking for updates.");
			return;
		}
		SetStatus("Checking for updates.");
		try
		{
			using HttpClient http = UpdateClient.CreateFeedClient("DEVI-Validate", ToolInfo.Version);
			using ECDsa key = UpdateTrust.CreatePublicKey();
			UpdateCheckResult updateCheckResult = await UpdateClient.CheckConfiguredAsync(http, "devi-validate", Version.Parse(ToolInfo.Version), key, CancellationToken.None);
			SetStatus(updateCheckResult.Message);
			UpdateWindow updateWindow = new UpdateWindow(updateCheckResult);
			updateWindow.Owner = this;
			updateWindow.ShowDialog();
		}
		catch (Exception ex)
		{
			CrashLog.Error("Manual update check failed", ex);
			SetStatus("Update check did not complete. Hashing still works offline.");
			MessageBox.Show(this, ex.Message, "Update check", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
	}

	private void HowTo_Click(object sender, RoutedEventArgs e)
	{
		HowToWindow howToWindow = new HowToWindow();
		howToWindow.Owner = this;
		howToWindow.ShowDialog();
	}

	private void Clear_Click(object sender, RoutedEventArgs e)
	{
		if (!_busy)
		{
			_evidence.Clear();
			_expectedInput.Clear();
			_manifest = null;
			_verification = null;
			_selfTest = null;
			_rows.Clear();
			ApplyProfileDefaults(includeAlgorithm: false);
			_showCaseErrors = false;
			UpdateCaseState();
			RefreshEvidence(focus: false);
			RefreshExpectedField(focusPaste: false);
			ShowIdle();
			ExportHtmlButton.IsEnabled = false;
			ExportJsonButton.IsEnabled = false;
			ExportPdfButton.IsEnabled = false;
			PackageButton.IsEnabled = _verification != null;
			HashProgress.Value = 0.0;
			HashProgress.Visibility = Visibility.Collapsed;
			StatusFilter.SelectedIndex = 0;
			HideSavedExport();
			SetStatus("Read-only. Hashing does not use the network.");
		}
	}

	private async void SelfTest_Click(object sender, RoutedEventArgs e)
	{
		OpenFolderDialog dialog = new OpenFolderDialog
		{
			Title = "Choose a folder for the self-test record",
			InitialDirectory = ExportFolder()
		};
		if (dialog.ShowDialog(this) != true)
		{
			return;
		}
		try
		{
			SetBusy(busy: true);
			_cancel = new CancellationTokenSource();
			string validatedBy = ValidatedByBox.Text;
			string validationDate = ValidationDateValue();
			string labProcedure = LabProcedureValue();
			foreach (string evidencePath in _evidence.Paths)
			{
				OutputPathGuard.EnsureOutside(evidencePath, dialog.FolderName);
			}
			string? evidence = _evidence.Paths.Count == 0 ? null : _evidence.Paths[0];
			CancellationToken token = _cancel.Token;
			SelfTestRecord selfTestRecord = await Task.Run(() => SelfTestRunner.RunAsync(ExaminationContext.Capture(null, null, validatedBy, validationDate, labProcedure), token), token);
			WrittenReports writtenReports = RecordStore.WriteSelfTest(evidence, dialog.FolderName, selfTestRecord);
			_selfTest = selfTestRecord;
			_verification = null;
			ShowSelfTest(selfTestRecord);
			ShowSavedExport(writtenReports.PdfPath ?? writtenReports.HtmlPath ?? writtenReports.JsonPath ?? "");
			SetStatus(selfTestRecord.Result + "  Tested " + TimeDisplay.Format(selfTestRecord.TestedAt, selfTestRecord.TimeZone));
			ExportHtmlButton.IsEnabled = true;
			ExportJsonButton.IsEnabled = true;
			ExportPdfButton.IsEnabled = true;
			PackageButton.IsEnabled = _verification != null;
		}
		catch (OperationCanceledException)
		{
			SetStatus("Canceled.");
		}
		catch (InvalidOperationException ex2) when (ex2.Message == "Reports must be saved outside the evidence location. A report file saved inside that folder is added to the set and changes the folder hash. Choose another folder, such as Documents\\DEVI Validate.")
		{
			ShowRefusedExport(ex2.Message);
		}
		catch (Exception ex3) when (((ex3 is InvalidDataException || ex3 is InvalidOperationException || ex3 is IOException || ex3 is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			SetStatus(ex3.Message);
		}
		finally
		{
			SetBusy(busy: false);
			_cancel?.Dispose();
			_cancel = null;
		}
	}

	private void ChooseEvidenceFile_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Choose evidence files",
			CheckFileExists = true,
			Multiselect = true
		};
		if (openFileDialog.ShowDialog(this) == true)
		{
			AddEvidence(openFileDialog.FileNames);
		}
	}

	private void ChooseEvidenceFolder_Click(object sender, RoutedEventArgs e)
	{
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "Choose evidence folders",
			Multiselect = true
		};
		if (openFolderDialog.ShowDialog(this) == true)
		{
			AddEvidence(openFolderDialog.FolderNames);
		}
	}

	private void ChooseExpected_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Choose expected hashes",
			CheckFileExists = true,
			Multiselect = true
		};
		if (openFileDialog.ShowDialog(this) == true)
		{
			AddExpected(openFileDialog.FileNames);
		}
	}

	private void ChooseExpectedFolder_Click(object sender, RoutedEventArgs e)
	{
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "Choose expected hash folders",
			Multiselect = true
		};
		if (openFolderDialog.ShowDialog(this) == true)
		{
			AddExpected(openFolderDialog.FolderNames);
		}
	}

	private void Evidence_DragOver(object sender, DragEventArgs e)
	{
		_evidenceHot = true;
		Zone_DragOver(e);
		PaintEvidence(drag: true);
	}

	private void Expected_DragOver(object sender, DragEventArgs e)
	{
		_expectedHot = true;
		Zone_DragOver(e);
		PaintExpected(drag: true);
	}

	private void Evidence_DragLeave(object sender, DragEventArgs e)
	{
		_evidenceHot = false;
		PaintEvidence(drag: false);
	}

	private void Expected_DragLeave(object sender, DragEventArgs e)
	{
		_expectedHot = false;
		PaintExpected(drag: false);
	}

	private void Evidence_MouseEnter(object sender, MouseEventArgs e)
	{
		if (!_evidenceHot)
		{
			EvidenceDash.Fill = (Brush)FindResource("ZoneHoverBrush");
			if (!_evidence.HasAny)
			{
				EvidenceDash.Stroke = (Brush)FindResource("LineStrongBrush");
			}
			PaintGlyph(EvidenceGlyph, emphasize: true);
		}
	}

	private void Evidence_MouseLeave(object sender, MouseEventArgs e)
	{
		if (!_evidenceHot)
		{
			PaintEvidence(drag: false);
			PaintGlyph(EvidenceGlyph, emphasize: false);
		}
	}

	private void Expected_MouseEnter(object sender, MouseEventArgs e)
	{
		if (!_expectedHot)
		{
			ExpectedDash.Fill = (Brush)FindResource("ZoneHoverBrush");
			if (!_expectedInput.HasFiles && _expectedInput.PastedText.Length == 0)
			{
				ExpectedDash.Stroke = (Brush)FindResource("LineStrongBrush");
			}
			PaintGlyph(ExpectedGlyph, emphasize: true);
		}
	}

	private void Expected_MouseLeave(object sender, MouseEventArgs e)
	{
		if (!_expectedHot)
		{
			PaintExpected(drag: false);
			PaintGlyph(ExpectedGlyph, emphasize: false);
		}
	}

	private void Evidence_Drop(object sender, DragEventArgs e)
	{
		_evidenceHot = false;
		PaintEvidence(drag: false);
		if (!_busy)
		{
			AddEvidence(DroppedPaths(e));
		}
	}

	private void Expected_Drop(object sender, DragEventArgs e)
	{
		_expectedHot = false;
		PaintExpected(drag: false);
		if (_busy)
		{
			return;
		}
		AddExpected(DroppedPaths(e));
	}

	private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_statusFilter = ((StatusFilter.SelectedItem as ComboBoxItem)?.Content as string) ?? "All";
		_view?.Refresh();
	}

	private void CopyHash_Click(object sender, RoutedEventArgs e)
	{
		if (!(sender is Button { DataContext: ResultRow dataContext }) || string.IsNullOrWhiteSpace(dataContext.ComputedHash))
		{
			SetStatus("There is no computed hash to copy.");
			return;
		}
		Clipboard.SetText(dataContext.ComputedHash);
		SetStatus("Copied the computed hash.");
	}

	private void ExportHtml_Click(object sender, RoutedEventArgs e)
	{
		Export(html: true, pdf: false);
	}

	private void ExportJson_Click(object sender, RoutedEventArgs e)
	{
		Export(html: false, pdf: false);
	}

	private void ExportPdf_Click(object sender, RoutedEventArgs e)
	{
		Export(html: false, pdf: true);
	}

	private void Package_Click(object sender, RoutedEventArgs e)
	{
		if (_verification == null)
		{
			SetStatus("Verify first. A validation package is built from a verification record.");
			return;
		}
		PackageWindow window = new PackageWindow(_verification, _evidence.Paths.Count == 0 ? null : _evidence.Paths[0], ExportFolder(), _evidence.Paths)
		{
			Owner = this
		};
		window.ShowDialog();
		if (window.CreatedFolder != null)
		{
			_lastExportFolder = window.CreatedFolder;
			SetStatus("Saved the validation package outside the evidence location.");
		}
	}

	private void VerifyPackage_Click(object sender, RoutedEventArgs e)
	{
		new VerifyPackageWindow
		{
			Owner = this
		}.ShowDialog();
	}

	private async Task RunAsync(bool verify)
	{
		if (!CaseReady)
		{
			ShowCaseRequired();
			return;
		}
		if (!_evidence.HasAny)
		{
			SetStatus("Choose an evidence file or folder.");
			return;
		}
		try
		{
			ExpectedHashes expected = null;
			string expectedSource = "";
			if (verify)
			{
				expected = BuildExpected();
				expectedSource = ExpectedSourceLabel();
				foreach (string path in _evidence.Paths)
				{
					ExpectedHashRules.EnsureCompatible(path, expected);
				}
			}
			SetBusy(busy: true);
			HideSavedExport();
			_cancel = new CancellationTokenSource();
			Progress<HashProgress> progress = new Progress<HashProgress>(UpdateProgress);
			HashAlgorithmKind algorithm = (verify ? expected.Algorithm : SelectedAlgorithm());
			string examiner = ExaminerBox.Text;
			string caseReference = CaseBox.Text;
			string agency = AgencyBox.Text;
			RememberExaminer(examiner, agency);
			CancellationToken token = _cancel.Token;
			List<string> evidencePaths = _evidence.Paths.ToList();
			List<HashManifest> manifests = new List<HashManifest>(evidencePaths.Count);
			foreach (string evidencePath in evidencePaths)
			{
				manifests.Add(await Task.Run(() => VerificationWorkflow.HashAsync(evidencePath, algorithm, examiner, caseReference, progress, token, agency), token));
			}
			_manifest = VerificationWorkflow.CombineManifests(manifests);
			if (verify)
			{
				_selfTest = null;
				_verification = VerificationWorkflow.VerifySelection(_manifest, expected, evidencePaths, expectedSource, ExaminerBox.Text, CaseBox.Text, ignorePathCase: false, ValidatedByBox.Text, ValidationDateValue(), LabProcedureValue(), AgencyBox.Text);
				_expectedInput.NoteCompared();
				ShowVerification(_verification);
			}
			else
			{
				_verification = null;
				_selfTest = null;
				ShowManifest(_manifest);
			}
		}
		catch (OperationCanceledException)
		{
			SetStatus("Canceled.");
		}
		catch (Exception ex2) when (((ex2 is InvalidDataException || ex2 is InvalidOperationException || ex2 is IOException || ex2 is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			SetStatus(ex2.Message);
		}
		finally
		{
			SetBusy(busy: false);
			_cancel?.Dispose();
			_cancel = null;
		}
	}

	/// <summary>Keeps the examiner name and agency for next time when the shared profile has none yet. Never the case number.</summary>
	private static void RememberExaminer(string examiner, string agency)
	{
		try
		{
			ExaminerProfile profile = ExaminerProfile.Load();
			bool changed = false;
			if (string.IsNullOrWhiteSpace(profile.ExaminerName) && !string.IsNullOrWhiteSpace(examiner))
			{
				profile.ExaminerName = examiner;
				changed = true;
			}
			if (string.IsNullOrWhiteSpace(profile.Agency) && !string.IsNullOrWhiteSpace(agency))
			{
				profile.Agency = agency;
				changed = true;
			}
			if (changed)
			{
				profile.Save();
			}
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
		{
			CrashLog.Error("Could not save the examiner profile (ignored)", ex);
		}
	}

	private ExpectedHashes BuildExpected()
	{
		ExpectedHashVerificationInput? input = _expectedInput.ForVerification();
		if (input == null)
		{
			throw new InvalidOperationException("Drop an expected hash file, or paste one hash for a single file.");
		}
		ExpectedReadOptions options = new ExpectedReadOptions
		{
			Algorithm = SelectedAlgorithm()
		};
		List<ExpectedHashes> parts = new List<ExpectedHashes>();
		foreach (ExpectedHashFile file in input.Files)
		{
			parts.Add(VerificationWorkflow.ReadExpectedPath(file.Path, options));
		}
		if (input.PastedText.Length > 0)
		{
			parts.Add(ReadPasted(input.PastedText));
		}
		return ExpectedHashSets.Combine(parts);
	}

	private string ExpectedSourceLabel()
	{
		ExpectedHashVerificationInput? input = _expectedInput.ForVerification();
		if (input == null)
		{
			return "";
		}
		List<string> parts = input.Files.Select((ExpectedHashFile file) => System.IO.Path.GetFullPath(file.Path)).ToList();
		if (input.PastedText.Length > 0)
		{
			parts.Add("pasted hash");
		}
		if (parts.Count == 1 && parts[0] == "pasted hash")
		{
			return "pasted hash";
		}
		return string.Join("; ", parts);
	}

	private ExpectedHashes ReadPasted(string text)
	{
		string trimmed = text.Trim();
		bool oneLine = trimmed.IndexOfAny(new char[2] { '\r', '\n' }) < 0;
		if (oneLine)
		{
			try
			{
				string hex = HashAlgorithms.NormalizeHex(trimmed);
				if (_evidence.Paths.Count != 1 || Directory.Exists(_evidence.Paths[0]))
				{
					throw new InvalidOperationException("A pasted hash applies to one file. Choose a file, or pass an expected hash list for a folder.");
				}
				return VerificationWorkflow.SingleHash(_evidence.Paths[0], hex, SelectedAlgorithm());
			}
			catch (InvalidDataException)
			{
			}
		}
		return ExpectedHashReader.ReadText(trimmed, null, new ExpectedReadOptions
		{
			Algorithm = SelectedAlgorithm()
		});
	}

	private HashAlgorithmKind SelectedAlgorithm()
	{
		if (!HashAlgorithms.TryParse(((AlgorithmBox.SelectedItem as ComboBoxItem)?.Content as string) ?? "SHA-256", out var kind))
		{
			throw new InvalidOperationException("Choose SHA-256, SHA-1, or MD5.");
		}
		return kind;
	}

	private string ValidationDateValue()
	{
		DateTime? selectedDate = ValidationDateBox.SelectedDate;
		if (selectedDate.HasValue)
		{
			return selectedDate.GetValueOrDefault().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
		return ValidationDateBox.Text ?? "";
	}

	private void ShowManifest(HashManifest manifest)
	{
		FillRows(manifest.Files.Select((FileHashEntry file) => new ResultRow
		{
			Result = "Hashed",
			Path = file.Path,
			Size = ByteSize.Format(file.Size),
			ComputedHash = file.Hash
		}).Concat(SkippedRows(manifest.Skipped)));
		VerdictKicker.Text = "Hash complete";
		VerdictText.Text = manifest.Files.Count.ToString(CultureInfo.InvariantCulture) + " files hashed with " + manifest.Algorithm + ".";
		if (manifest.Skipped.Count > 0)
		{
			TextBlock verdictText = VerdictText;
			verdictText.Text = verdictText.Text + " " + ReportCopy.SkippedSummary(manifest.Skipped.Count);
		}
		PaintVerdict("hash", "Hash only computed a new hash. It did not compare that hash with a physical analyzer, an FTK log, or any other earlier value. This is not a verification. To verify, add the other tool's hash in step 3 and press Verify.");
		SetCounts(manifest.Files.Count, 0, 0, 0, "Hashed", "Mismatch");
		SetStatus("Hashed " + TimeDisplay.Format(manifest.CreatedAt, manifest.TimeZone) + ". Export JSON to save the manifest outside the evidence location.");
		ExportHtmlButton.IsEnabled = true;
		ExportJsonButton.IsEnabled = true;
		ExportPdfButton.IsEnabled = false;
		PackageButton.IsEnabled = _verification != null;
		HashProgress.Value = 1.0;
		ExportsExpander.IsExpanded = true;
	}

	private void ShowVerification(VerificationRecord record)
	{
		FillRows(record.Files.Select(delegate(FileVerificationEntry file)
		{
			long? size = file.Size;
			string size2 = size.HasValue ? ByteSize.Format(size.GetValueOrDefault()) : "-";
			return new ResultRow
			{
				Result = ResultLabels.Display(file.Result),
				Path = file.Path,
				Size = size2,
				ComputedHash = file.ComputedHash ?? "",
				ExpectedHash = file.ExpectedHash ?? ""
			};
		}).Concat(SkippedRows(record.Skipped)));
		bool flag = record.MismatchCount > 0;
		VerdictKicker.Text = (flag ? "Does not match" : ((record.MissingCount > 0) ? "File not found" : ((record.ExtraCount > 0) ? "Review the file list" : ((record.Skipped.Count > 0) ? "Paths not hashed" : "Match"))));
		VerdictText.Text = record.Verdict;
		if (flag)
		{
			PaintVerdict("mismatch", "Red means the new hash is not the same as the hash written down before. The file we read is not the same as the file that hash describes. The notes below say what that can and cannot mean.");
		}
		else if (record.MissingCount > 0)
		{
			PaintVerdict("missing", "A file on the expected list was not among the files we read. Its hash was not checked. Missing is not a hash mismatch.");
		}
		else if (record.ExtraCount > 0)
		{
			PaintVerdict("review", "Every compared hash matched. Extra files were read and had no earlier hash to compare.");
		}
		else if (record.Skipped.Count > 0)
		{
			PaintVerdict("missing", "Some paths were not hashed. Read the Skipped rows before treating this as a complete check.");
		}
		else
		{
			PaintVerdict("match", "A match means the new hash is the same as the hash written down before. The file we read is the same file that hash describes. Anyone can check the file again with sha256sum, certutil -hashfile, or Get-FileHash. This check verifies the bytes only. It does not show who made the file, what the file means, or whether a chain of custody is complete.");
		}
		SetCounts(record.MatchCount, record.MismatchCount, record.MissingCount, record.ExtraCount, "Match", "Mismatch");
		SetStatus("Verified " + TimeDisplay.Format(record.VerifiedAt, record.TimeZone) + "    Match " + record.MatchCount.ToString(CultureInfo.InvariantCulture) + "    Mismatch " + record.MismatchCount.ToString(CultureInfo.InvariantCulture) + "    Missing " + record.MissingCount.ToString(CultureInfo.InvariantCulture) + "    Extra " + record.ExtraCount.ToString(CultureInfo.InvariantCulture));
		ExportHtmlButton.IsEnabled = true;
		ExportJsonButton.IsEnabled = true;
		ExportPdfButton.IsEnabled = true;
		PackageButton.IsEnabled = _verification != null;
		HashProgress.Value = 1.0;
		ExportsExpander.IsExpanded = true;
	}

	private void ShowSelfTest(SelfTestRecord record)
	{
		FillRows(record.Checks.Select((SelfTestCheck check) => new ResultRow
		{
			Result = (check.Passed ? "Pass" : "Fail"),
			Path = check.Name + "  " + check.Input,
			ComputedHash = check.Computed,
			ExpectedHash = check.Expected
		}));
		VerdictKicker.Text = ((record.FailedCount == 0) ? "Pass" : "Fail");
		VerdictText.Text = record.Result;
		PaintVerdict((record.FailedCount == 0) ? "match" : "mismatch", (record.FailedCount == 0) ? "This is a tool validation record from the built-in self-test. It is not an evidence verification. The checks hash published test vectors for SHA-256, SHA-1, and MD5, and they confirm that a synthetic file's contents and timestamps were unchanged after hashing. A passing result means this build reproduced the published values and the read-only check on this machine. It does not certify the tool for casework." : null);
		SetCounts(record.PassedCount, record.FailedCount, 0, 0, "Pass", "Fail");
	}

	private void ShowIdle()
	{
		VerdictKicker.Text = "No verification yet";
		VerdictText.Text = "Fill in Case details, add the evidence and the expected hash, then press Verify.";
		PaintVerdict("idle", null);
		SetCounts(0, 0, 0, 0, "Match", "Mismatch");
		CountsGrid.Visibility = Visibility.Collapsed;
	}

	private void PaintVerdict(string kind, string? meaning)
	{
		Brush background;
		Brush borderBrush;
		Brush foreground;
		Brush brush;
		switch (kind)
		{
		case "match":
			background = (Brush)FindResource("SuccessSoftBrush");
			borderBrush = (Brush)FindResource("SuccessBrush");
			foreground = (Brush)FindResource("SuccessBrush");
			brush = (Brush)FindResource("TextBrush");
			break;
		case "mismatch":
			background = (Brush)FindResource("DangerSoftBrush");
			borderBrush = (Brush)FindResource("DangerBrush");
			foreground = (Brush)FindResource("DangerBrush");
			brush = (Brush)FindResource("TextBrush");
			break;
		case "missing":
			background = (Brush)FindResource("WarningSoftBrush");
			borderBrush = (Brush)FindResource("WarningBrush");
			foreground = (Brush)FindResource("WarningBrush");
			brush = (Brush)FindResource("TextBrush");
			break;
		case "review":
		case "hash":
			background = (Brush)FindResource("AccentSoftBrush");
			borderBrush = (Brush)FindResource("AccentBrush");
			foreground = (Brush)FindResource("AccentTextBrush");
			brush = (Brush)FindResource("TextBrush");
			break;
		default:
			background = (Brush)FindResource("CardBrush");
			borderBrush = (Brush)FindResource("LineBrush");
			foreground = (Brush)FindResource("MutedBrush");
			brush = (Brush)FindResource("TextBrush");
			break;
		}
		VerdictBand.Background = background;
		VerdictBand.BorderBrush = borderBrush;
		VerdictKicker.Foreground = foreground;
		VerdictText.Foreground = brush;
		if (string.IsNullOrEmpty(meaning))
		{
			VerdictMeaning.Visibility = Visibility.Collapsed;
			return;
		}
		VerdictMeaning.Text = meaning;
		TextBlock verdictMeaning = VerdictMeaning;
		verdictMeaning.Foreground = (Brush)FindResource("SecondaryBrush");
		VerdictMeaning.Visibility = Visibility.Visible;
	}

	private void SetCounts(int match, int mismatch, int missing, int extra, string matchLabel, string mismatchLabel)
	{
		CountsGrid.Visibility = Visibility.Visible;
		MatchCountText.Text = match.ToString(CultureInfo.InvariantCulture);
		MismatchCountText.Text = mismatch.ToString(CultureInfo.InvariantCulture);
		MissingCountText.Text = missing.ToString(CultureInfo.InvariantCulture);
		ExtraCountText.Text = extra.ToString(CultureInfo.InvariantCulture);
		MatchChipLabel.Text = matchLabel;
		MismatchChipLabel.Text = mismatchLabel;
		MissingChipLabel.Text = "Missing";
		ExtraChipLabel.Text = "Extra";
	}

	private static IEnumerable<ResultRow> SkippedRows(IEnumerable<SkippedEntry> skipped)
	{
		return skipped.Select((SkippedEntry entry) => new ResultRow
		{
			Result = "Skipped",
			Path = entry.Path,
			Size = "-",
			ComputedHash = ReportCopy.SkippedDetail(entry)
		});
	}

	private void FillRows(IEnumerable<ResultRow> rows)
	{
		_rows.Clear();
		foreach (ResultRow row in rows)
		{
			_rows.Add(row);
		}
		_view?.Refresh();
	}

	private bool FilterRow(object item)
	{
		if (_statusFilter == "All" || !(item is ResultRow resultRow))
		{
			return item is ResultRow;
		}
		return string.Equals(resultRow.Result, _statusFilter, StringComparison.Ordinal);
	}

	private void UpdateProgress(HashProgress progress)
	{
		string text = (progress.Fraction * 100.0).ToString("0", CultureInfo.InvariantCulture);
		HashProgress.Value = progress.Fraction;
		string text2 = ((progress.TotalBytes > 0) ? (" of " + ByteSize.Format(progress.TotalBytes)) : "");
		SetStatus("File " + progress.FileIndex.ToString(CultureInfo.InvariantCulture) + " of " + progress.FileCount.ToString(CultureInfo.InvariantCulture) + "  |  " + ByteSize.Format(progress.BytesRead) + text2 + "  |  " + text + "%  |  " + progress.RelativePath);
	}

	private void Export(bool html, bool pdf)
	{
		if (_selfTest == null && (!_evidence.HasAny || _manifest == null))
		{
			SetStatus("Hash, verify, or run the self-test before exporting.");
			return;
		}
		try
		{
			string extension = (pdf ? ".pdf" : (html ? ".html" : ".json"));
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Title = (pdf ? "Export PDF record" : (html ? "Export HTML record" : "Export JSON")),
				FileName = SuggestedName(extension),
				Filter = (pdf ? "PDF (*.pdf)|*.pdf" : (html ? "HTML (*.html)|*.html" : "JSON (*.json)|*.json")),
				OverwritePrompt = false,
				InitialDirectory = ExportFolder()
			};
			if (saveFileDialog.ShowDialog(this) == true)
			{
				string outputFile = ExportNames.Unused(saveFileDialog.FileName);
				foreach (string evidencePath in _evidence.Paths)
				{
					OutputPathGuard.EnsureOutside(evidencePath, outputFile);
				}
				string? anchor = _evidence.Paths.Count == 0 ? null : _evidence.Paths[0];
				string path = ((_selfTest != null) ? (pdf ? RecordStore.WriteSelfTestPdf(anchor, outputFile, _selfTest) : (html ? RecordStore.WriteSelfTestHtml(anchor, outputFile, _selfTest) : RecordStore.WriteSelfTestJson(anchor, outputFile, _selfTest))) : ((_verification == null) ? (html ? RecordStore.WriteManifestHtml(anchor, outputFile, _manifest) : RecordStore.WriteManifest(anchor, outputFile, _manifest)) : (pdf ? RecordStore.WriteVerificationPdf(anchor, outputFile, _verification) : (html ? RecordStore.WriteVerificationHtml(anchor, outputFile, _verification) : RecordStore.WriteVerificationJson(anchor, outputFile, _verification)))));
				ShowSavedExport(path);
				SetStatus("Saved the report outside the evidence location.");
			}
		}
		catch (InvalidOperationException ex) when (ex.Message == "Reports must be saved outside the evidence location. A report file saved inside that folder is added to the set and changes the folder hash. Choose another folder, such as Documents\\DEVI Validate.")
		{
			ShowRefusedExport(ex.Message);
		}
		catch (Exception ex2) when (((ex2 is InvalidOperationException || ex2 is IOException || ex2 is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			SetStatus(ex2.Message);
		}
	}

	private void ShowRefusedExport(string message)
	{
		string text = OutputPathGuard.SuggestOutputDirectory(_evidence.Paths, _lastExportFolder);
		SetStatus(message, warning: true);
		MessageBox.Show(this, message + "\n\nSuggested folder:\n" + text, "Save the report outside the evidence", MessageBoxButton.OK, MessageBoxImage.Exclamation);
	}

	private void ShowSavedExport(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			_lastExportFile = path;
			string directoryName = System.IO.Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				_lastExportFolder = directoryName;
			}
			ExportPathText.Text = path;
			ExportSavedPanel.Visibility = Visibility.Visible;
		}
	}

	private void HideSavedExport()
	{
		ExportSavedPanel.Visibility = Visibility.Collapsed;
		ExportPathText.Text = "";
		_lastExportFile = null;
	}

	private string ExportFolder()
	{
		string text = OutputPathGuard.SuggestOutputDirectory(_evidence.Paths, _lastExportFolder);
		try
		{
			Directory.CreateDirectory(text);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		if (!Directory.Exists(text))
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		}
		return text;
	}

	private void OpenExportedFile_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_lastExportFile) || !File.Exists(_lastExportFile))
		{
			SetStatus("The saved report is no longer at that path.");
			return;
		}
		Process.Start(new ProcessStartInfo(_lastExportFile)
		{
			UseShellExecute = true
		});
	}

	private void OpenExportedFolder_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_lastExportFile) || !File.Exists(_lastExportFile))
		{
			SetStatus("The saved report is no longer at that path.");
			return;
		}
		Process.Start(new ProcessStartInfo
		{
			FileName = "explorer.exe",
			Arguments = "/select,\"" + _lastExportFile + "\"",
			UseShellExecute = true
		});
	}

	private void SetStatus(string text, bool warning = false)
	{
		StatusText.Text = text;
		StatusText.Foreground = (Brush)FindResource(warning ? "WarningBrush" : "SecondaryBrush");
	}

	private string SuggestedName(string extension)
	{
		if (_selfTest != null)
		{
			return ExportNames.SelfTestStem(_selfTest.Version, _selfTest.TestedAt) + extension;
		}
		if (_verification != null)
		{
			return ExportNames.Stem(_verification) + extension;
		}
		if (!(extension == ".html"))
		{
			if (extension == ".json")
			{
				return "DEVI-Validate-manifest.json";
			}
			return "DEVI-Validate-manifest.pdf";
		}
		return "DEVI-Validate-manifest.html";
	}

	private void AddEvidence(IReadOnlyList<string> paths)
	{
		if (_busy || paths.Count == 0)
		{
			return;
		}
		int added = _evidence.Add(paths);
		RefreshEvidence(focus: true);
		if (added > 0)
		{
			DiscardRun();
		}
		SetStatus(added == 0 ? "Already selected." : "Added " + added.ToString(CultureInfo.InvariantCulture) + (added == 1 ? " item." : " items."));
	}

	private void AddExpected(IReadOnlyList<string> paths)
	{
		if (_busy || paths.Count == 0)
		{
			return;
		}
		string current = LabProcedureValue();
		bool keepSource = current.Length > 0 && !string.Equals(current, _autoLabProcedure, StringComparison.Ordinal);
		int added = _expectedInput.AddFiles(paths, DescribeExpected, keepSource);
		RefreshExpectedField(focusPaste: true);
		SetStatus(added == 0 ? "Already selected." : "Added " + added.ToString(CultureInfo.InvariantCulture) + (added == 1 ? " hash item." : " hash items."));
	}

	private static (ExpectedHashes? Parsed, string? SuggestedSource) DescribeExpected(string path)
	{
		try
		{
			ExpectedHashes parsed = VerificationWorkflow.ReadExpectedPath(path, new ExpectedReadOptions());
			return (parsed, ExpectedHashSources.Suggest(parsed.FormatName, path));
		}
		catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException || ex is FormatException)
		{
			// The file is checked again, with a message, when Verify runs.
			return (null, null);
		}
	}

	private void EvidenceChipRemove_Click(object sender, RoutedEventArgs e)
	{
		if (_busy || sender is not Button { Tag: string path })
		{
			return;
		}
		if (!_evidence.Remove(path))
		{
			return;
		}
		RefreshEvidence(focus: true);
		DiscardRun();
		SetStatus("Removed the evidence item.");
	}

	private void Evidence_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (_busy || e.Key != Key.Escape)
		{
			return;
		}
		if (!_evidence.HandleEscape())
		{
			return;
		}
		e.Handled = true;
		RefreshEvidence(focus: true);
		DiscardRun();
		SetStatus("Cleared the evidence.");
	}

	private void ExpectedChipRemove_Click(object sender, RoutedEventArgs e)
	{
		if (_busy || sender is not Button { Tag: string path })
		{
			return;
		}
		if (!_expectedInput.RemoveFile(path))
		{
			return;
		}
		RefreshExpectedField(focusPaste: true);
		SetStatus("Removed the expected hash.");
	}

	private void ExpectedHash_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (_busy || e.Key != Key.Escape)
		{
			return;
		}
		if (!_expectedInput.HandleEscape())
		{
			return;
		}
		e.Handled = true;
		RefreshExpectedField(focusPaste: true);
		SetStatus("Cleared the expected hash.");
	}

	private void PastedHash_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (_syncingExpected)
		{
			return;
		}
		_expectedInput.SetPastedText(PastedHashBox.Text);
		RefreshExpectedField(focusPaste: false);
	}

	private void PastedHash_FocusChanged(object sender, RoutedEventArgs e)
	{
		UpdateExpectedHint();
	}

	/// <summary>
	/// Paints the chip and the drop-zone label from <see cref="ExpectedHashInput"/>.
	/// A value the examiner typed in Lab procedure is left in place. A value this window filled from the file is removed with the file.
	/// </summary>
	private void RefreshExpectedField(bool focusPaste)
	{
		_syncingExpected = true;
		try
		{
			if (!string.Equals(PastedHashBox.Text, _expectedInput.PastedText, StringComparison.Ordinal))
			{
				PastedHashBox.Text = _expectedInput.PastedText;
			}
		}
		finally
		{
			_syncingExpected = false;
		}
		PastedHashBox.IsReadOnly = !_expectedInput.FieldIsEditable;
		List<PathChip> chips = _expectedInput.Files.Select((ExpectedHashFile file) => new PathChip(file.Path, "Remove expected hash", "Expected hash")).ToList();
		ExpectedFileChips.ItemsSource = chips;
		ExpectedFileChips.Visibility = chips.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
		ExpectedText.Text = "Drop the log, sum file, CSV, or JSON from the other tool";
		ExpectedText.Foreground = (Brush)FindResource("SecondaryBrush");
		ExpectedText.ToolTip = null;
		string meta = DescribePaths(_expectedInput.Files.Select((ExpectedHashFile file) => file.Path));
		ExpectedMeta.Text = meta;
		ExpectedMeta.Visibility = string.IsNullOrEmpty(meta) ? Visibility.Collapsed : Visibility.Visible;
		UpdateExpectedHint();
		PaintExpected(drag: false);
		SyncLabProcedure();
		DiscardVerificationIfNeeded();
		if (focusPaste)
		{
			PastedHashBox.Focus();
		}
	}

	private void UpdateExpectedHint()
	{
		bool show = string.IsNullOrEmpty(PastedHashBox.Text) && !PastedHashBox.IsKeyboardFocused;
		ExpectedHashHint.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
	}

	private void SyncLabProcedure()
	{
		string shown = LabProcedureValue();
		bool shownIsAuto = _autoLabProcedure != null && string.Equals(shown, _autoLabProcedure, StringComparison.Ordinal);
		string? suggestion = _expectedInput.SuggestedSource;
		if (suggestion == null)
		{
			if (shownIsAuto)
			{
				LabProcedureBox.SelectedIndex = -1;
				LabProcedureBox.Text = "";
			}
			_autoLabProcedure = null;
			return;
		}
		if (shown.Length == 0 || shownIsAuto)
		{
			LabProcedureBox.SelectedItem = suggestion;
			LabProcedureBox.Text = suggestion;
			_autoLabProcedure = suggestion;
		}
	}

	private void RefreshEvidence(bool focus)
	{
		List<PathChip> chips = _evidence.Paths.Select((string path) => new PathChip(path, "Remove evidence item", "Evidence item")).ToList();
		EvidenceChips.ItemsSource = chips;
		EvidenceChips.Visibility = chips.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
		EvidenceText.Text = "Drop the file or folder the first hash describes";
		EvidenceText.Foreground = (Brush)FindResource("SecondaryBrush");
		EvidenceText.ToolTip = null;
		string meta = DescribePaths(_evidence.Paths);
		EvidenceMeta.Text = meta;
		EvidenceMeta.Visibility = string.IsNullOrEmpty(meta) ? Visibility.Collapsed : Visibility.Visible;
		PaintEvidence(drag: false);
		if (focus)
		{
			EvidenceZone.Focus();
		}
	}

	private void DiscardVerificationIfNeeded()
	{
		if (_expectedInput.HasComparison || (_verification == null && _manifest == null))
		{
			return;
		}
		DiscardRun();
		SetStatus("Cleared the previous comparison.");
	}

	private void DiscardRun()
	{
		if (_manifest == null && _verification == null)
		{
			return;
		}
		_manifest = null;
		_verification = null;
		_rows.Clear();
		ShowIdle();
		ExportHtmlButton.IsEnabled = false;
		ExportJsonButton.IsEnabled = false;
		ExportPdfButton.IsEnabled = false;
		PackageButton.IsEnabled = false;
		HashProgress.Value = 0.0;
		HashProgress.Visibility = Visibility.Collapsed;
		StatusFilter.SelectedIndex = 0;
	}

	private static string DescribeSelection(string path)
	{
		if (Directory.Exists(path))
		{
			return "Folder";
		}
		if (File.Exists(path))
		{
			return "File  |  " + ByteSize.Format(new FileInfo(path).Length);
		}
		return "";
	}

	private static string DescribePaths(IEnumerable<string> paths)
	{
		List<string> list = paths.ToList();
		if (list.Count == 0)
		{
			return "";
		}
		if (list.Count == 1)
		{
			return DescribeSelection(list[0]);
		}
		int folders = list.Count(Directory.Exists);
		int files = list.Count(File.Exists);
		List<string> parts = new List<string>();
		if (files > 0)
		{
			parts.Add(files.ToString(CultureInfo.InvariantCulture) + (files == 1 ? " file" : " files"));
		}
		if (folders > 0)
		{
			parts.Add(folders.ToString(CultureInfo.InvariantCulture) + (folders == 1 ? " folder" : " folders"));
		}
		int other = list.Count - files - folders;
		if (other > 0)
		{
			parts.Add(other.ToString(CultureInfo.InvariantCulture) + (other == 1 ? " item" : " items"));
		}
		return string.Join(", ", parts);
	}

	private void PaintEvidence(bool drag)
	{
		PaintZone(EvidenceDash, drag, _evidence.HasAny);
		PaintGlyph(EvidenceGlyph, drag || EvidenceZone.IsMouseOver);
	}

	private void PaintExpected(bool drag)
	{
		PaintZone(ExpectedDash, drag, _expectedInput.HasFiles || _expectedInput.PastedText.Length > 0);
		PaintGlyph(ExpectedGlyph, drag || ExpectedZone.IsMouseOver);
	}

	private void PaintGlyph(System.Windows.Shapes.Path glyph, bool emphasize)
	{
		glyph.Stroke = (Brush)FindResource(emphasize ? "AccentBrush" : "MutedBrush");
	}

	private void PaintZone(Rectangle dash, bool drag, bool selected)
	{
		dash.StrokeDashArray = null;
		if (drag)
		{
			dash.Fill = (Brush)FindResource("ZoneDragBrush");
			dash.Stroke = (Brush)FindResource("AccentBrush");
			dash.StrokeThickness = 2.0;
		}
		else
		{
			dash.Fill = (Brush)FindResource("CardBrush");
			dash.Stroke = (Brush)FindResource(selected ? "AccentBrush" : "BorderBrush");
			dash.StrokeThickness = (selected ? 1.5 : 1.0);
		}
	}

	private static void Zone_DragOver(DragEventArgs e)
	{
		e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None);
		e.Handled = true;
	}

	private static IReadOnlyList<string> DroppedPaths(DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			return Array.Empty<string>();
		}
		if (e.Data.GetData(DataFormats.FileDrop) is not string[] array || array.Length == 0)
		{
			return Array.Empty<string>();
		}
		return array;
	}

	private void SetBusy(bool busy)
	{
		_busy = busy;
		UpdateCaseState();
		SelfTestMenuItem.IsEnabled = !busy;
		CancelButton.IsEnabled = busy;
		CancelButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
		if (busy)
		{
			HashProgress.Visibility = Visibility.Visible;
		}
		ClearButton.IsEnabled = !busy;
		ExaminerBox.IsEnabled = !busy;
		AgencyBox.IsEnabled = !busy;
		CaseBox.IsEnabled = !busy;
		ValidatedByBox.IsEnabled = !busy;
		ValidationDateBox.IsEnabled = !busy;
		LabProcedureBox.IsEnabled = !busy;
		AlgorithmBox.IsEnabled = !busy;
		PastedHashBox.IsEnabled = !busy;
		PastedHashBox.IsReadOnly = false;
		EvidenceChips.IsEnabled = !busy;
		ExpectedFileChips.IsEnabled = !busy;
		ExpectedFolderButton.IsEnabled = !busy;
		EvidenceFileButton.IsEnabled = !busy;
		EvidenceFolderButton.IsEnabled = !busy;
		ExpectedFileButton.IsEnabled = !busy;
		if (busy)
		{
			ExportHtmlButton.IsEnabled = false;
			ExportJsonButton.IsEnabled = false;
			ExportPdfButton.IsEnabled = false;
			PackageButton.IsEnabled = _verification != null;
		}
	}

}
