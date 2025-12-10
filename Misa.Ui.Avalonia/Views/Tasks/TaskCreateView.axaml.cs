using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Misa.Ui.Avalonia.ViewModels.Tasks;

namespace Misa.Ui.Avalonia.Views.Tasks;

public partial class TaskCreateView : UserControl
{
    public TaskCreateView()
    {
        InitializeComponent();
    }
    public void TitleTextChangedEvent(object sender, TextChangedEventArgs e)
    {
        if (DataContext is not TaskCreateViewModel viewModel) return;
        
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