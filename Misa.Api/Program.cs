using Misa.Infrastructure.Data;
using Misa.Contract.Items;
using Microsoft.EntityFrameworkCore;
using Misa.Api.Common.Exceptions;
using Misa.Api.Common.Realtime;
using Misa.Api.Endpoints.Entities;
using Misa.Api.Endpoints.Items;
using Misa.Api.Endpoints.Scheduling;
using Misa.Application.Common.Abstractions.Events;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Entities.Commands;
using Misa.Application.Entities.Commands.Description;
using Misa.Application.Entities.Queries;
using Misa.Application.Entities.Queries.GetSingleDetailedEntity;
using Misa.Application.Items.Commands;
using Misa.Application.Items.Queries;
using Misa.Application.ReferenceData.Queries;
using Misa.Application.Scheduling.Commands.Deadlines;
using Misa.Contract.Entities;
using Misa.Contract.Items.Common;
using Misa.Infrastructure.Persistence.Repositories;
using Wolverine;

const string connectionString =
    "Host=localhost;Port=5432;Database=misa;Username=postgres;Password=meow";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddTransient<ExceptionMappingMiddleware>();
builder.Services.AddDbContext<MisaDbContext>(options => options.UseNpgsql(connectionString));

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(RemoveItemDeadlineHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(UpsertItemDeadlineHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetTasksHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GetItemDetailsHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(AddDescriptionHandler).Assembly);
    opts.Discovery.IncludeAssembly(typeof(StartSessionHandler).Assembly);
});

builder.Services.AddScoped<EventsHub>();
builder.Services.AddScoped<IEventPublisher, SignalREventPublisher>();



builder.Services.AddScoped<CreateItemHandler>();
builder.Services.AddScoped<GetLookupsHandler>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IMainRepository, MainRepository>();

// Entity

builder.Services.AddScoped<GetEntitiesHandler>();
builder.Services.AddScoped<SessionHandler>();
builder.Services.AddScoped<AddEntityHandler>();
builder.Services.AddScoped<PatchEntityHandler>();
builder.Services.AddScoped<UpdateItemHandler>();
builder.Services.AddScoped<GetSingleDetailedEntityHandler>();
builder.Services.AddScoped<IEntityRepository, EntityRepository>();




var app = builder.Build();
app.MapControllers();
app.MapHub<EventsHub>("/hubs/events");
app.UseMiddleware<ExceptionMappingMiddleware>();

app.MapGet("/api/entities/{id:guid}", 
    async (Guid id, GetSingleDetailedEntityHandler handler) 
    => await handler.Handle(id));

app.MapPatch("/Entity/Delete", async (Guid entityId, PatchEntityHandler handler, CancellationToken ct = default) 
    => await handler.DeleteEntityAsync(entityId, ct));
app.MapPatch("/Entity/Archive", async (Guid entityId, PatchEntityHandler handler, CancellationToken ct = default) 
    => await handler.ArchiveEntityAsync(entityId, ct));

app.MapGet("/api/lookups", async ( GetLookupsHandler lookupsHandler, CancellationToken ct) 
    => await lookupsHandler.GetAllAsync(ct));
app.MapGet("/Lookups/UserSettableStates", async ( int stateId, GetLookupsHandler lookupsHandler, CancellationToken ct ) 
    => await lookupsHandler.GetUserSettableStates(stateId, ct));

app.MapGet("/api/entities/get", async ( GetEntitiesHandler handler, CancellationToken ct) 
    => await handler.GetAllAsync(ct));

app.MapPost("/api/entities/add", async (
    CreateEntityDto dto,
    AddEntityHandler handler,
    CancellationToken ct) =>
{
    var entity = dto.Transform();
    await handler.AddAsync(entity, ct);
    return Results.Ok();
});

app.MapPost("/api/tasks", async ( CreateItemDto dto, CreateItemHandler itemHandler, CancellationToken ct) 
    => await itemHandler.AddTaskAsync(dto, ct));
app.MapPatch("/tasks", async (UpdateItemDto dto, UpdateItemHandler handler) =>
{
    await handler.UpdateAsync(dto);
    return Results.Ok();
});

// Session
// app.MapPost("/Sessions/Start", async (SessionDto dto, SessionHandler handler) 
//     => await handler.StartSessionAsync(dto));
// app.MapPost("/Sessions/Pause", async (PauseSessionDto dto, SessionHandler handler) 
//     => await handler.PauseSessionAsync(dto));
// app.MapPost(
//     "/Sessions/Continue/{entityId:guid}",
//     async (Guid entityId, SessionHandler handler)
//         => await handler.ContinueSessionAsync(entityId)
// );
// app.MapPost(
//     "/Sessions/Stop",
//     async (StopSessionDto dto, SessionHandler handler)
//         => await handler.StopSessionAsync(dto)
// );

TaskEndpoints.Map(app);
ItemDetailEndpoints.Map(app);
DeadlineEndpoints.Map(app);
DescriptionEndpoints.Map(app);

app.Run();
