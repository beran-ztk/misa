using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class LayerHostView : UserControl
{
    public LayerHostView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Dispatcher.UIThread.Post(FocusFirstInput, DispatcherPriority.Input);
    }

    private void FocusFirstInput()
    {
        var first = this.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(t => t.IsEffectivelyVisible && t.IsEffectivelyEnabled && t.IsHitTestVisible);
        first?.Focus();
    }
}
