using Microsoft.EntityFrameworkCore;
using Misa.Api.Common.Exceptions;
using Misa.Api.Common.Realtime;
using Misa.Api.Endpoints.Entities;
using Misa.Api.Endpoints.Items;
using Misa.Api.Endpoints.Scheduling;
using Misa.Api.Services.Features.Items.Features.Sessions;
using Misa.Application.Common.Abstractions.Events;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Base.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Base.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Base.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Features.Deadlines.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Queries;
using Misa.Application.Features.Entities.Features.Descriptions.Commands;
using Misa.Application.ReferenceData.Queries;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
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
        // "Host=localhost;Port=5432;Database=misa;Username=postgres;Password=meow",
        "Host=localhost;Port=5432;Database=misa;Username=misa;Password=misa",
        npgsql =>
        {
            npgsql.MapEnum<ScheduleMisfirePolicy>("schedule_misfire_policy");
            npgsql.MapEnum<ScheduleFrequencyType>("schedule_frequency_type");
            npgsql.MapEnum<Priority>("priority");
            npgsql.MapEnum<ChangeType>("change_type");
            npgsql.MapEnum<Workflow>("workflow");
            npgsql.MapEnum<SessionState>("session_state");
            npgsql.MapEnum<SessionEfficiencyType>("session_efficiency_type");
            npgsql.MapEnum<SessionConcentrationType>("session_concentration_type");
        }
    ));

builder.Services.AddHostedService<SessionAutostopWorker>();
builder.Services.AddHostedService<SessionPastMaxTimeWorker>();

builder.Services.AddSignalR();
builder.Services.AddScoped<EventsHub>();
builder.Services.AddScoped<IEventPublisher, SignalREventPublisher>();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(RemoveItemDeadlineHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(UpsertItemDeadlineHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetTasksHandler).Assembly);
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
});

// Registrations
builder.Services.AddScoped<CreateItemHandler>();
builder.Services.AddScoped<PatchEntityHandler>();

builder.Services.AddScoped<IEntityRepository, EntityRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IMainRepository, MainRepository>();




var app = builder.Build();
app.MapControllers();
app.MapHub<EventsHub>("/hubs/events");
app.UseMiddleware<ExceptionMappingMiddleware>();

TaskEndpoints.Map(app);
ItemDetailEndpoints.Map(app);
DeadlineEndpoints.Map(app);
DescriptionEndpoints.Map(app);
SchedulingEndpoints.Map(app);

app.MapPatch("/Entity/Delete", async (Guid entityId, PatchEntityHandler handler, CancellationToken ct = default) 
    => await handler.DeleteEntityAsync(entityId, ct));
app.MapPatch("/Entity/Archive", async (Guid entityId, PatchEntityHandler handler, CancellationToken ct = default) 
    => await handler.ArchiveEntityAsync(entityId, ct));

app.MapGet("/Lookups/UserSettableStates", async ( int stateId, GetLookupsHandler lookupsHandler, CancellationToken ct ) 
    => await lookupsHandler.GetUserSettableStates(stateId, ct));

app.MapPost("/api/tasks", async ( CreateItemDto dto, CreateItemHandler itemHandler, CancellationToken ct) 
    => await itemHandler.AddTaskAsync(dto, ct));


app.Run();
