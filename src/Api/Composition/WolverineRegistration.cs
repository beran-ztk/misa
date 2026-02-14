using Misa.Application.Features.Authentication;
using Misa.Application.Features.Common.Deadlines;
using Misa.Application.Features.Entities.Extensions.Items.Base.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Queries;
using Misa.Application.Features.Entities.Features.Descriptions.Commands;
using Wolverine;

namespace Misa.Api.Composition;

public static class WolverineRegistration
{
    public static IHostBuilder RegisterHandlersToWolverine(this IHostBuilder host)
    {
        host.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(AddDescriptionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(DeleteDescriptionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(GetItemDetailsHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(GetCurrentSessionDetailsHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(StartSessionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(PauseSessionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(ContinueSessionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(StopSessionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(PauseDueSessionsHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(PauseExpiredSessionsHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(AddScheduleHandler).Assembly);

            opts.Discovery.IncludeAssembly(typeof(GetSchedulesHandler).Assembly);

            opts.Discovery.IncludeAssembly(typeof(SchedulePlanningHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(ScheduleExecutingHandler).Assembly);
            
            opts.Discovery.IncludeAssembly(typeof(LoginHandler).Assembly); 
            opts.Discovery.IncludeAssembly(typeof(RegisterHandler).Assembly);
            
            // Task
            opts.Discovery.IncludeAssembly(typeof(CreateTaskHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(GetTasksHandler).Assembly);
            
            // Deadline
            opts.Discovery.IncludeAssembly(typeof(UpsertDeadlineHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(DeleteDeadlineHandler).Assembly);
        });

        return host;
    }
}