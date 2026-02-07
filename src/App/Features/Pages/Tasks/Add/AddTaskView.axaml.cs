using Avalonia.Controls;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Add;
public partial class AddTaskView : Window
{
    public AddTaskView()
    {
        InitializeComponent();
    }
    public void TitleTextChangedEvent(object sender, TextChangedEventArgs e)
    {
        if (DataContext is not Pages.Tasks.Add.AddTaskViewModel viewModel) return;

        if (string.IsNullOrEmpty(viewModel.Title)) return;
        viewModel.TitleHasValidationError = false;
    }
}