using Avalonia.Controls;

namespace Misa.Ui.Avalonia.Features.Tasks.Add;
public partial class AddTaskView : Window
{
    public AddTaskView()
    {
        InitializeComponent();
    }
    public void TitleTextChangedEvent(object sender, TextChangedEventArgs e)
    {
        if (DataContext is not AddTaskViewModel viewModel) return;

        if (string.IsNullOrEmpty(viewModel.Title)) return;
        viewModel.TitleHasValidationError = false;
    }
}