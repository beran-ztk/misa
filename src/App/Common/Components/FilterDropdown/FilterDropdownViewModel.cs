using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Misa.Ui.Avalonia.Common.Components.FilterDropdown;

public sealed partial class FilterDropdownViewModel : ObservableObject
{
    public string Title { get; }
    public ObservableCollection<FilterOption> Options { get; }

    /// <summary>Raised whenever any option's selection changes.</summary>
    public event EventHandler? FilterChanged;

    public FilterDropdownViewModel(string title, IEnumerable<string> optionLabels)
    {
        Title = title;
        Options = new ObservableCollection<FilterOption>(
            optionLabels.Select(l => new FilterOption(l)));

        foreach (var option in Options)
            option.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FilterOption.IsSelected))
                    OnFilterOptionChanged();
            };
    }

    public bool HasActiveSelection => Options.Any(o => o.IsSelected);

    public string SummaryText
    {
        get
        {
            var selected = Options.Where(o => o.IsSelected).ToList();
            return selected.Count switch
            {
                0 => string.Empty,
                1 => selected[0].Label,
                _ => $"{selected.Count} sel."
            };
        }
    }

    public IEnumerable<string> SelectedLabels =>
        Options.Where(o => o.IsSelected).Select(o => o.Label);

    [RelayCommand]
    private void Reset()
    {
        foreach (var o in Options)
            o.IsSelected = false;
    }

    private void OnFilterOptionChanged()
    {
        OnPropertyChanged(nameof(HasActiveSelection));
        OnPropertyChanged(nameof(SummaryText));
        FilterChanged?.Invoke(this, EventArgs.Empty);
    }
}
