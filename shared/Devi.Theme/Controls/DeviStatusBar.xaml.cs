using System.Windows;
using System.Windows.Controls;

namespace Devi.Theme.Controls;

/// <summary>The footer strip on every DEVI main window.</summary>
public partial class DeviStatusBar : UserControl
{
    public static readonly DependencyProperty LeftTextProperty = DependencyProperty.Register(
        nameof(LeftText), typeof(string), typeof(DeviStatusBar), new PropertyMetadata("", (d, e) => ((DeviStatusBar)d).LeftTextBlock.Text = (string)e.NewValue));

    public static readonly DependencyProperty RightTextProperty = DependencyProperty.Register(
        nameof(RightText), typeof(string), typeof(DeviStatusBar), new PropertyMetadata("", (d, e) => ((DeviStatusBar)d).RightTextBlock.Text = (string)e.NewValue));

    public DeviStatusBar()
    {
        InitializeComponent();
    }

    public string LeftText
    {
        get => (string)GetValue(LeftTextProperty);
        set => SetValue(LeftTextProperty, value);
    }

    public string RightText
    {
        get => (string)GetValue(RightTextProperty);
        set => SetValue(RightTextProperty, value);
    }
}
