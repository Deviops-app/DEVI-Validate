using System.Windows;
using System.Windows.Controls;

namespace Devi.Theme.Controls;

/// <summary>
/// The header at the top of every DEVI main window: a mono eyebrow, the page title in the
/// white-to-gray heading fill, a one-line lead, and the window's actions on the right.
/// A templated control (not a UserControl) so named action controls stay in the window's
/// name scope. The template lives in Themes/Controls.xaml under the DeviPageHeader type.
/// </summary>
public sealed class DeviPageHeader : ContentControl
{
    public static readonly DependencyProperty EyebrowProperty = DependencyProperty.Register(
        nameof(Eyebrow), typeof(string), typeof(DeviPageHeader), new PropertyMetadata(""));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(DeviPageHeader), new PropertyMetadata(""));

    public static readonly DependencyProperty LeadProperty = DependencyProperty.Register(
        nameof(Lead), typeof(string), typeof(DeviPageHeader), new PropertyMetadata(""));

    static DeviPageHeader()
    {
        FocusableProperty.OverrideMetadata(typeof(DeviPageHeader), new FrameworkPropertyMetadata(false));
    }

    public string Eyebrow
    {
        get => (string)GetValue(EyebrowProperty);
        set => SetValue(EyebrowProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Lead
    {
        get => (string)GetValue(LeadProperty);
        set => SetValue(LeadProperty, value);
    }
}
