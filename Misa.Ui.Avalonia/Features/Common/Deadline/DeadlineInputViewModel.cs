using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Common.Deadline;

public sealed partial class DeadlineInputViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private bool _createWithParent = true;

    [ObservableProperty] private DateTimeOffset _dueDate = DateTimeOffset.UtcNow.Date;
    [ObservableProperty] private TimeSpan _dueTime = DateTimeOffset.UtcNow.TimeOfDay;

    public DeadlineInputDto? ToDtoOrNull()
    {
        if (!IsEnabled)
            return null;

        var dueAtUtc = DueDate
            .Add(DueTime)
            .ToUniversalTime();

        return new DeadlineInputDto(dueAtUtc);
    }

    public void Reset()
    {
        IsEnabled = false;
        DueDate = DateTimeOffset.UtcNow.Date;
        DueTime = DateTimeOffset.UtcNow.TimeOfDay;
    }
}