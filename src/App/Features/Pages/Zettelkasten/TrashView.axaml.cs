using Avalonia.Controls;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class TrashView : UserControl
{
    public TrashView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is TrashViewModel vm)
                _ = vm.LoadAsync();
        };
    }
}
