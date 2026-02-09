using System;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Add;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.Client;

public class PanelProxy(ShellState shellState, IServiceProvider serviceProvider)
{
    public void Close() => shellState.Panel = null;
    
    public void OpenAddTask(object dataContext)
    {
        var service = serviceProvider.GetRequiredService<AddTaskView>();
        service.DataContext = dataContext;
        
        shellState.Panel = service;
    }
}