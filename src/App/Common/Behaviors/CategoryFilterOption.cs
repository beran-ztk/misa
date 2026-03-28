using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public sealed partial class CategoryFilterOption : ObservableObject
{
    public TaskCategory TaskCategory { get; }
    [ObservableProperty] private bool _isSelected;

    public CategoryFilterOption(TaskCategory taskCategory, bool isSelected = true)
    {
        TaskCategory = taskCategory;
        IsSelected = isSelected;
    }
}
