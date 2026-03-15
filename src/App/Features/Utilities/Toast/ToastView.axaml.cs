using Avalonia.Controls;
using Avalonia.Input;

namespace Misa.Ui.Avalonia.Features.Utilities.Toast;

public partial class ToastView : UserControl
{
    public ToastView()
    {
        InitializeComponent();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        (DataContext as ToastViewModel)?.PauseAutoDismiss();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        (DataContext as ToastViewModel)?.ResumeAutoDismiss();
    }
}
