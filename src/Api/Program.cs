using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Misa.Api.Common.Exceptions;
using Misa.Api.Common.Hubs;
using Misa.Api.Endpoints.Features.Authentication;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Base;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Extensions;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Features;
using Misa.Api.Endpoints.Features.Entities.Features;
using Misa.Api.Services.Features.Items.Features.Scheduler;
using Misa.Api.Services.Features.Items.Features.Sessions;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Authentication;
using Misa.Application.Features.Entities.Extensions.Items.Base.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Queries;
using Misa.Application.Features.Entities.Features.Descriptions.Commands;
using Misa.Contract.Common.Results;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Domain.Features.Messaging;
using Misa.Infrastructure.Persistence.Context;
using Misa.Infrastructure.Persistence.Repositories;
using Wolverine;
using Priority = Misa.Domain.Features.Entities.Extensions.Items.Base.Priority;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddTransient<ExceptionMappingMiddleware>();

builder.Services.AddDbContext<DefaultContext>(options =>
    options.UseNpgsql(
        "Host=localhost;Port=5432;Database=misa;Username=postgres;Password=meow",
        npgsql =>
        {
            npgsql.MapEnum<ScheduleMisfirePolicy>();
            npgsql.MapEnum<ScheduleFrequencyType>();
            npgsql.MapEnum<ScheduleActionType>();
            npgsql.MapEnum<SchedulerExecutionStatus>();
            npgsql.MapEnum<Priority>();
            npgsql.MapEnum<ChangeType>();
            npgsql.MapEnum<Workflow>();
            npgsql.MapEnum<SessionState>();
            npgsql.MapEnum<SessionEfficiencyType>();
            npgsql.MapEnum<SessionConcentrationType>();
            npgsql.MapEnum<TaskCategory>();
            npgsql.MapEnum<EventType>();
            npgsql.MapEnum<OutboxEventState>();
        }
    ));

builder.Services.AddHostedService<SessionAutostopWorker>();
builder.Services.AddHostedService<SessionPastMaxTimeWorker>();
builder.Services.AddHostedService<SchedulePlanningWorker>();
builder.Services.AddHostedService<ScheduleExecutingWorker>();
builder.Services.AddHostedService<SchedulePublishingWorker>();

builder.Services.AddScoped<IEntityRepository, EntityRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<ISchedulerPlanningRepository, SchedulerPlanningRepository>();
builder.Services.AddScoped<ISchedulerExecutingRepository, SchedulerExecutingRepository>();
builder.Services.AddScoped<ISchedulerRepository, SchedulerRepository>();
builder.Services.AddScoped<ISchedulerRepository, SchedulerRepository>();
builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();

// DI
builder.Host.UseWolverine(opts =>
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
    
    // Tasks
    opts.Discovery.IncludeAssembly(typeof(AddTaskHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetTasksHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetSchedulesHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CreateOnceScheduleHandler).Assembly);
    
    opts.Discovery.IncludeAssembly(typeof(SchedulePlanningHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(ScheduleExecutingHandler).Assembly);
    
    opts.Discovery.IncludeAssembly(typeof(LoginHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(RegisterHandler).Assembly);
});

// build app
var app = builder.Build();
app.MapHub<EventHub>("/hubs/updates");
app.MapControllers();
app.UseMiddleware<ExceptionMappingMiddleware>();

TaskEndpoints.Map(app);
ItemDetailEndpoints.Map(app);
DescriptionEndpoints.Map(app);
SchedulingEndpoints.Map(app);
AuthEndpoints.Map(app);

app.Run();
