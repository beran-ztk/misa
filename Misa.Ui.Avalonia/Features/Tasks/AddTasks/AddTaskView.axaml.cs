using Avalonia.Controls;

namespace Misa.Ui.Avalonia.Features.Tasks.AddTasks;

public partial class AddTaskView : UserControl
{
    public AddTaskView()
    {
        InitializeComponent();
    }
    public void TitleTextChangedEvent(object sender, TextChangedEventArgs e)
    {
        if (DataContext is not AddTaskViewModel viewModel) return;
        
        if (!string.IsNullOrEmpty(viewModel.Title))
        {
            viewModel.ErrorMessageTitle = null;
            viewModel.TitleBorderBrush = null;
        }
        else
        {
            viewModel.TitleError();
        }
    }
}