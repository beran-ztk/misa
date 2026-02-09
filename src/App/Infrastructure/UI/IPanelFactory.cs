using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public enum PanelKey
{
    Task,
    Schedule
}

public interface IPanelFactory
{
    Control Create(PanelKey key, object? context);
}

public sealed class PanelFactory(IServiceProvider serviceProvider) : IPanelFactory
{

    public Control Create(PanelKey key, object? context)
    {
        Control view;
        
        switch (key)
        {
            case PanelKey.Task:
                view = serviceProvider.GetRequiredService<CreateTaskView>();
                view.DataContext = context ?? serviceProvider.GetRequiredService<TaskFacadeViewModel>();
                break;
            case PanelKey.Schedule:
                view = serviceProvider.GetRequiredService<CreateScheduleView>();
                view.DataContext = context ?? serviceProvider.GetRequiredService<SchedulerFacadeViewModel>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        return view;
    }
}