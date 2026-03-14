using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public sealed partial class CategoryFilterOption : ObservableObject
{
    public TaskCategoryDto TaskCategory { get; }
    [ObservableProperty] private bool _isSelected;

    public CategoryFilterOption(TaskCategoryDto taskCategory, bool isSelected = true)
    {
        TaskCategory = taskCategory;
        IsSelected = isSelected;
    }
}
