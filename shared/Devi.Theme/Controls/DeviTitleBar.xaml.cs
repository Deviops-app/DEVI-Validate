using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Devi.Theme.Controls;

/// <summary>
/// The title bar used by every DEVI window. It shows the white DEVI wordmark, the tool name,
/// an optional detail (the version on a main window, the dialog name on a dialog), and the
/// caption buttons. The window must use the DeviWindow or DeviDialog style so the caption
/// height matches.
/// </summary>
public partial class DeviTitleBar : UserControl
{
    public static readonly DependencyProperty ToolNameProperty = DependencyProperty.Register(
        nameof(ToolName), typeof(string), typeof(DeviTitleBar), new PropertyMetadata("", (d, e) => ((DeviTitleBar)d).ToolNameText.Text = (string)e.NewValue));

    public static readonly DependencyProperty DetailProperty = DependencyProperty.Register(
        nameof(Detail), typeof(string), typeof(DeviTitleBar), new PropertyMetadata("", (d, e) => ((DeviTitleBar)d).DetailText.Text = (string)e.NewValue));

    public static readonly DependencyProperty ShowMinimizeProperty = DependencyProperty.Register(
        nameof(ShowMinimize), typeof(bool), typeof(DeviTitleBar), new PropertyMetadata(true, (d, e) => ((DeviTitleBar)d).MinimizeButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

    public static readonly DependencyProperty ShowMaximizeProperty = DependencyProperty.Register(
        nameof(ShowMaximize), typeof(bool), typeof(DeviTitleBar), new PropertyMetadata(true, (d, e) => ((DeviTitleBar)d).MaximizeButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

    private Window? _window;

    public DeviTitleBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Raised when the close button is pressed. When nobody handles it, the window closes.</summary>
    public event EventHandler<RoutedEventArgs>? CloseClicked;

    public string ToolName
    {
        get => (string)GetValue(ToolNameProperty);
        set => SetValue(ToolNameProperty, value);
    }

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public bool ShowMinimize
    {
        get => (bool)GetValue(ShowMinimizeProperty);
        set => SetValue(ShowMinimizeProperty, value);
    }

    public bool ShowMaximize
    {
        get => (bool)GetValue(ShowMaximizeProperty);
        set => SetValue(ShowMaximizeProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _window = Window.GetWindow(this);
        if (_window is null)
        {
            return;
        }

        _window.StateChanged += Window_StateChanged;
        UpdateMaximizeIcon();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_window is not null)
        {
            _window.StateChanged -= Window_StateChanged;
        }
    }

    private void Window_StateChanged(object? sender, EventArgs e) => UpdateMaximizeIcon();

    private void UpdateMaximizeIcon()
    {
        var maximized = _window?.WindowState == WindowState.Maximized;
        MaximizeIcon.Data = (Geometry)FindResource(maximized ? "IconCaptionRestore" : "IconCaptionMaximize");
        MaximizeButton.ToolTip = maximized ? "Restore" : "Maximize";
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        if (_window is not null)
        {
            _window.WindowState = WindowState.Minimized;
        }
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (_window is not null)
        {
            _window.WindowState = _window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (CloseClicked is not null)
        {
            CloseClicked(this, e);
            return;
        }

        _window?.Close();
    }
}
