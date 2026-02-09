using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public enum ModalKey
{
    Task
}
public interface IModalFactory
{
    Control Create(ModalKey key, object? context);
}

public sealed class ModalFactory(IServiceProvider serviceProvider) : IModalFactory
{
    public Control Create(ModalKey key, object? context)
    {
        Control view;
        
        switch (key)
        {
            case ModalKey.Task:
                view = serviceProvider.GetRequiredService<CreateTaskView>();
                view.DataContext = context ?? serviceProvider.GetRequiredService<TaskFacadeViewModel>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key));
        }

        return view;
    }
}