using System.Windows;
using System.Windows.Controls;

namespace Devi.Theme.Controls;

/// <summary>
/// The single border around a DEVI text field. The template paints a filled rounded
/// rectangle in the ring color, then a smaller one in the field color. The gap is the
/// border, so the corner stays the same thickness as the sides. A stroked
/// <see cref="System.Windows.Controls.Border.BorderBrush"/> does not: the stroke thins
/// where the corner bends, and a second focus rectangle draws another box.
/// </summary>
/// <remarks>
/// Set <see cref="IsActive"/> from a control template with
/// <c>TemplateBinding</c> to the templated parent's keyboard focus. A template child
/// does not see that focus through its own <see cref="UIElement.IsKeyboardFocusWithin"/>.
/// A field that wraps other controls, such as the expected-hash box, can leave
/// <see cref="IsActive"/> false and rely on its own focus-within state.
/// </remarks>
public sealed class FieldRing : ContentControl
{
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(FieldRing),
        new FrameworkPropertyMetadata(false));

    static FieldRing()
    {
        FocusableProperty.OverrideMetadata(typeof(FieldRing), new FrameworkPropertyMetadata(false));
    }

    /// <summary>
    /// True while the surrounding field has keyboard focus. The control template binds this
    /// because the ring itself is not the focused element.
    /// </summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}
