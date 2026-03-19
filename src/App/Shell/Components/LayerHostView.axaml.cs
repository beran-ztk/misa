using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class LayerHostView : UserControl
{
    private IInputElement? _previousFocus;

    public LayerHostView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _previousFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        Dispatcher.UIThread.Post(FocusFirstInput, DispatcherPriority.Input);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        (_previousFocus as Control)?.Focus();
    }

    private void FocusFirstInput()
    {
        var first = this.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(t => t.IsEffectivelyVisible && t.IsEffectivelyEnabled && t.IsHitTestVisible);
        first?.Focus();
    }
}
