using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public class FocusWhenVisibleBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<FocusWhenVisibleBehavior, Control, bool>("Enabled");

    static FocusWhenVisibleBehavior()
    {
        EnabledProperty.Changed.AddClassHandler<Control>(OnEnabledChanged);
    }

    private static void OnEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not true)
            return;

        void TryFocus()
        {
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

        control.GetObservable(Visual.IsVisibleProperty)
            .Subscribe(new AnonymousObserver<bool>(_ => TryFocus()));
        TryFocus();
    }

    public static void SetEnabled(AvaloniaObject element, bool value)
        => element.SetValue(EnabledProperty, value);

    public static bool GetEnabled(AvaloniaObject element)
        => element.GetValue(EnabledProperty);
}