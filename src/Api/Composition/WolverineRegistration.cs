using Misa.Application.Features.Authentication;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Application.Features.Items.Schedules.Commands;
using Misa.Application.Features.Items.Sessions.Commands;
using Misa.Application.Features.Items.Sessions.Queries;
using Misa.Application.Features.Items.Tasks;
using Wolverine;

namespace Misa.Api.Composition;

public static class WolverineRegistration
{
    public static IHostBuilder RegisterHandlersToWolverine(this IHostBuilder host)
    {
        host.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(GetCurrentSessionDetailsHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(StartSessionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(PauseSessionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(ContinueSessionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(StopSessionHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(PauseDueSessionsHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(PauseExpiredSessionsHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(CreateScheduleHandler).Assembly);

            opts.Discovery.IncludeAssembly(typeof(GetSchedulesHandler).Assembly);

            opts.Discovery.IncludeAssembly(typeof(SchedulePlanningHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(ScheduleExecutingHandler).Assembly);
            
            opts.Discovery.IncludeAssembly(typeof(LoginHandler).Assembly); 
            opts.Discovery.IncludeAssembly(typeof(RegisterHandler).Assembly);
            
            // Task
            opts.Discovery.IncludeAssembly(typeof(CreateTaskHandler).Assembly);
            opts.Discovery.IncludeAssembly(typeof(GetTasksHandler).Assembly);
        });

        return host;
    }
}