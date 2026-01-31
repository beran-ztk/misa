using Microsoft.EntityFrameworkCore;
using Misa.Api.Common.Exceptions;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Base;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Extensions;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Features;
using Misa.Api.Endpoints.Features.Entities.Features;
using Misa.Api.Services.Features.Items.Features.Scheduler;
using Misa.Api.Services.Features.Items.Features.Sessions;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Extensions.Items.Base.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Queries;
using Misa.Application.Features.Entities.Features.Descriptions.Commands;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Infrastructure.Persistence.Context;
using Misa.Infrastructure.Persistence.Repositories;
using Wolverine;
using Priority = Misa.Domain.Features.Entities.Extensions.Items.Base.Priority;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddTransient<ExceptionMappingMiddleware>();

builder.Services.AddDbContext<DefaultContext>(options =>
    options.UseNpgsql(
        "Host=localhost;Port=5432;Database=misa;Username=postgres;Password=meow",
        npgsql =>
        {
            npgsql.MapEnum<ScheduleMisfirePolicy>("schedule_misfire_policy");
            npgsql.MapEnum<ScheduleFrequencyType>("schedule_frequency_type");
            npgsql.MapEnum<ScheduleActionType>("schedule_action_type");
            npgsql.MapEnum<SchedulerExecutionStatus>("schedule_execution_state");
            npgsql.MapEnum<Priority>("priority");
            npgsql.MapEnum<ChangeType>("change_type");
            npgsql.MapEnum<Workflow>("workflow");
            npgsql.MapEnum<SessionState>("session_state");
            npgsql.MapEnum<SessionEfficiencyType>("session_efficiency_type");
            npgsql.MapEnum<SessionConcentrationType>("session_concentration_type");
            npgsql.MapEnum<TaskCategory>("task_category");
        }
    ));

builder.Services.AddHostedService<SessionAutostopWorker>();
builder.Services.AddHostedService<SessionPastMaxTimeWorker>();
builder.Services.AddHostedService<SchedulePlanningWorker>();
builder.Services.AddHostedService<ScheduleExecutingWorker>();

builder.Services.AddScoped<IEntityRepository, EntityRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<ISchedulerPlanningRepository, SchedulerPlanningRepository>();
builder.Services.AddScoped<ISchedulerExecutingRepository, SchedulerExecutingRepository>();
builder.Services.AddScoped<ISchedulerRepository, SchedulerRepository>();

// DI
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(AddDescriptionHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetItemDetailsHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetCurrentSessionDetailsHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(StartSessionHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(PauseSessionHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(ContinueSessionHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(StopSessionHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(PauseDueSessionsHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(PauseExpiredSessionsHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(AddScheduleHandler).Assembly);
    
    // Tasks
    opts.Discovery.IncludeAssembly(typeof(AddTaskHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetTasksHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetSchedulesHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CreateOnceScheduleHandler).Assembly);
    
    opts.Discovery.IncludeAssembly(typeof(SchedulePlanningHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(ScheduleExecutingHandler).Assembly);
});

// build app
var app = builder.Build();
app.MapControllers();
app.UseMiddleware<ExceptionMappingMiddleware>();

TaskEndpoints.Map(app);
ItemDetailEndpoints.Map(app);
DescriptionEndpoints.Map(app);
SchedulingEndpoints.Map(app);

app.Run();
