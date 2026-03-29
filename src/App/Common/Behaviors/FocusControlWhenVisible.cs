using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Misa.App.Common.Behaviors;

public sealed class FocusControlWhenVisible : AvaloniaObject
{
    public static readonly AttachedProperty<bool> FocusOnVisibleProperty =
        AvaloniaProperty.RegisterAttached<FocusControlWhenVisible, Control, bool>("FocusOnVisible");

    static FocusControlWhenVisible()
    {
        FocusOnVisibleProperty.Changed.AddClassHandler<Control>(OnEnabledChanged);
    }

    private static void OnEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not true)
            return;

        if (!control.IsVisible || !control.Focusable)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (control is { IsVisible: true , Focusable: true })
                control.Focus();
            if (control is TextBox textBox)
                textBox.CaretIndex = textBox.Text?.Length ?? 0;
        });
    }

    public static void SetFocusOnVisible(AvaloniaObject element, bool value)
        => element.SetValue(FocusOnVisibleProperty, value);

    public static bool GetFocusOnVisible(AvaloniaObject element)
        => element.GetValue(FocusOnVisibleProperty);
}