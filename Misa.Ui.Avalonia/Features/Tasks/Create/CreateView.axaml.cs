using Avalonia.Controls;

namespace Misa.Ui.Avalonia.Features.Tasks.Create;

public partial class CreateView : UserControl
{
    public CreateView()
    {
        InitializeComponent();
    }
    public void TitleTextChangedEvent(object sender, TextChangedEventArgs e)
    {
        if (DataContext is not CreateViewModel viewModel) return;

        if (string.IsNullOrEmpty(viewModel.Title)) return;
        viewModel.TitleHasValidationError = false;
    }
}