using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Converters;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Common.Components.DeadlinePicker;

public sealed partial class DeadlinePickerViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRelativeMode))]
    [NotifyPropertyChangedFor(nameof(IsCustomMode))]
    [NotifyPropertyChangedFor(nameof(SelectedDeadline))]
    private DeadlinePickerMode _mode = DeadlinePickerMode.Relative;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDeadline))]
    private DeadlinePreset? _selectedPreset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDeadline))]
    private DateTimeOffset? _customDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDeadline))]
    private TimeSpan? _customTime;

    public bool IsRelativeMode => Mode == DeadlinePickerMode.Relative;
    public bool IsCustomMode => Mode == DeadlinePickerMode.Custom;

    public DateTimeOffset? SelectedDeadline => Mode switch
    {
        DeadlinePickerMode.Relative when SelectedPreset is not null
            => SelectedPreset.Resolve().ToUniversalTime(),
        DeadlinePickerMode.Custom
            => DateTimeOffsetHelper.CombineLocalDateAndTimeToUtc(CustomDate, CustomTime),
        _ => null
    };

    public IReadOnlyList<DeadlinePreset> Presets { get; } = BuildPresets();

    [RelayCommand]
    private void SetRelativeMode() => Mode = DeadlinePickerMode.Relative;

    [RelayCommand]
    private void SetCustomMode()
    {
        SelectedPreset = null;
        Mode = DeadlinePickerMode.Custom;
    }

    partial void OnSelectedPresetChanged(DeadlinePreset? value)
    {
        if (value is not null)
            Mode = DeadlinePickerMode.Relative;
    }

    partial void OnCustomDateChanged(DateTimeOffset? value)
    {
        if (value is not null && CustomTime is null)
            CustomTime = TimeSpan.Zero;
    }

    public void InitializeFrom(DateTimeOffset? deadline)
    {
        if (deadline is null)
        {
            SelectedPreset = null;
            CustomDate = null;
            CustomTime = null;
            Mode = DeadlinePickerMode.Relative;
            return;
        }

        var local = deadline.Value.ToLocalTime();
        CustomDate = new DateTimeOffset(DateTime.SpecifyKind(local.Date, DateTimeKind.Local));
        CustomTime = local.TimeOfDay;
        Mode = DeadlinePickerMode.Custom;
    }

    private static List<DeadlinePreset> BuildPresets() =>
    [
        new("In 1 hour",  () => DateTimeOffset.Now.AddHours(1)),
        new("In 6 hours", () => DateTimeOffset.Now.AddHours(6)),
        new("Tomorrow",   () =>
        {
            var tomorrow9am = DateTime.Today.AddDays(1).AddHours(9);
            return new DateTimeOffset(DateTime.SpecifyKind(tomorrow9am, DateTimeKind.Local));
        }),
        new("In 3 days",  () => DateTimeOffset.Now.AddDays(3)),
        new("In 1 week",  () => DateTimeOffset.Now.AddDays(7)),
        new("In 1 month", () => DateTimeOffset.Now.AddMonths(1)),
    ];
}

public enum DeadlinePickerMode
{
    Relative,
    Custom
}

