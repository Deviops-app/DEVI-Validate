using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace DeviValidate.Desktop;

public partial class AboutWindow : Window
{

	public bool RequestedUpdateCheck { get; private set; }

	public AboutWindow()
	{
		InitializeComponent();
		VersionText.Text = DeviValidate.Core.ToolInfo.Version;
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		Devi.Theme.DeviWindowChrome.Attach(this);
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void CheckUpdates_Click(object sender, RoutedEventArgs e)
	{
		RequestedUpdateCheck = true;
		Close();
	}

}
