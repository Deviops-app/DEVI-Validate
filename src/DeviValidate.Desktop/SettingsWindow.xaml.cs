using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Devi.Theme.Profile;

namespace DeviValidate.Desktop;

/// <summary>Settings: the shared examiner profile, Validate defaults, and updates. Stored only on this computer.</summary>
public partial class SettingsWindow : Window
{
	public bool RequestedUpdateCheck { get; private set; }

	public bool Saved { get; private set; }

	public SettingsWindow()
	{
		InitializeComponent();
		foreach (string source in ExpectedHashSources.All)
		{
			DefaultLabProcedureBox.Items.Add(source);
		}
		ProfileEditor.Show(ExaminerProfile.Load());
		ValidateSettings settings = ValidateSettings.Load();
		DefaultValidatedByBox.Text = settings.DefaultValidatedBy ?? "";
		DefaultLabProcedureBox.Text = settings.DefaultLabProcedure ?? "";
		SelectAlgorithm(settings.DefaultAlgorithm);
		CheckOnLaunchBox.IsChecked = settings.CheckOnLaunch;
		StorageText.Text = "Settings stay on this computer and are never sent anywhere. The profile is in " + DeviLocalStore.ProfilePath + " and the Validate settings are in " + ValidateSettings.FilePath + ". No case number, evidence path, or hash is saved.";
		Loaded += (_, _) => ProfileEditor.FocusFirst();
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		try
		{
			Devi.Theme.DeviWindowChrome.Attach(this);
		}
		catch (Exception ex)
		{
			CrashLog.Error("Settings chrome attach failed (continuing)", ex);
		}
	}

	private void SelectAlgorithm(string? name)
	{
		ComboBoxItem? item = DefaultAlgorithmBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => string.Equals(i.Content as string, name, StringComparison.Ordinal));
		DefaultAlgorithmBox.SelectedItem = item ?? DefaultAlgorithmBox.Items[0];
	}

	private void DefaultLabProcedure_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (DefaultLabProcedureBox.SelectedItem as string == ExpectedHashSources.Other)
		{
			Dispatcher.BeginInvoke(() =>
			{
				DefaultLabProcedureBox.SelectedIndex = -1;
				DefaultLabProcedureBox.Text = "";
				DefaultLabProcedureBox.Focus();
			});
		}
	}

	private bool SaveAll()
	{
		try
		{
			ProfileEditor.Read().Save();
			ValidateSettings settings = ValidateSettings.Load();
			settings.DefaultValidatedBy = DefaultValidatedByBox.Text;
			settings.DefaultLabProcedure = DefaultLabProcedureBox.Text == ExpectedHashSources.Other ? null : DefaultLabProcedureBox.Text;
			settings.DefaultAlgorithm = (DefaultAlgorithmBox.SelectedItem as ComboBoxItem)?.Content as string;
			settings.CheckOnLaunch = CheckOnLaunchBox.IsChecked == true;
			settings.Save();
			Saved = true;
			return true;
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
		{
			SaveStatus.Text = "Could not save the settings: " + ex.Message;
			return false;
		}
	}

	private void Save_Click(object sender, RoutedEventArgs e)
	{
		if (SaveAll())
		{
			DialogResult = true;
		}
	}

	private void CheckNow_Click(object sender, RoutedEventArgs e)
	{
		if (SaveAll())
		{
			RequestedUpdateCheck = true;
			DialogResult = true;
		}
	}

	private void ClearProfile_Click(object sender, RoutedEventArgs e)
	{
		MessageBoxResult answer = MessageBox.Show(this, "Delete the saved examiner profile and the Validate defaults from this computer? Other DEVI apps on this computer use the same profile.", "Clear saved profile", MessageBoxButton.OKCancel, MessageBoxImage.Question);
		if (answer != MessageBoxResult.OK)
		{
			return;
		}
		try
		{
			ExaminerProfile.Clear();
			ValidateSettings settings = ValidateSettings.Load();
			settings.DefaultValidatedBy = null;
			settings.DefaultLabProcedure = null;
			settings.DefaultAlgorithm = null;
			settings.Save();
			ProfileEditor.Show(new ExaminerProfile());
			DefaultValidatedByBox.Text = "";
			DefaultLabProcedureBox.Text = "";
			SelectAlgorithm(null);
			Saved = true;
			SaveStatus.Text = "The saved profile was cleared.";
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
		{
			SaveStatus.Text = "Could not clear the profile: " + ex.Message;
		}
	}
}
