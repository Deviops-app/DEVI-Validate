using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Desktop;

public partial class HowToWindow : Window
{

	public HowToWindow()
	{
		InitializeComponent();
		Brush foreground = (Brush)FindResource("TextBrush");
		Brush foreground2 = (Brush)FindResource("SecondaryBrush");
		foreach (ReportSection section in VerificationGuide.Sections)
		{
			Sections.Children.Add(new TextBlock
			{
				Text = section.Heading,
				FontSize = 16.0,
				FontWeight = FontWeights.SemiBold,
				Foreground = foreground,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 16.0, 0.0, 6.0)
			});
			string[] array = section.Body.Split("\n\n");
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length != 0)
				{
					Sections.Children.Add(new TextBlock
					{
						Text = text,
						Foreground = foreground2,
						TextWrapping = TextWrapping.Wrap,
						Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
						LineHeight = 22.0
					});
				}
			}
		}
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		Devi.Theme.DeviWindowChrome.Attach(this);
		Rect workArea = SystemParameters.WorkArea;
		base.MaxHeight = Math.Max(base.MinHeight, workArea.Height - 32.0);
		base.MaxWidth = Math.Max(base.MinWidth, workArea.Width - 32.0);
		if (base.Height > base.MaxHeight)
		{
			base.Height = base.MaxHeight;
		}
		if (base.Width > base.MaxWidth)
		{
			base.Width = base.MaxWidth;
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

}
