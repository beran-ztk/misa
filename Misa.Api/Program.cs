using Misa.Infrastructure.Data;
using Misa.Infrastructure.Items;
using Misa.Application.Items.Repositories;
using Misa.Contract.Items;
using Microsoft.EntityFrameworkCore;
using Misa.Application.Entities.Add;
using Misa.Application.Entities.Get;
using Misa.Application.Entities.Repositories;
using Misa.Application.Items.Add;
using Misa.Application.Items.Get;
using Misa.Application.Items.Patch;
using Misa.Application.Main.Add;
using Misa.Application.Main.Get;
using Misa.Application.Main.Repositories;
using Misa.Contract.Audit;
using Misa.Contract.Entities;
using Misa.Contract.Main;
using Misa.Infrastructure.Entities;
using Misa.Infrastructure.Main;

const string connectionString =
    "Host=localhost;Port=5432;Database=misa;Username=postgres;Password=meow";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MisaDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<CreateItemHandler>();
builder.Services.AddScoped<GetLookupsHandler>();
builder.Services.AddScoped<GetItemsHandler>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IMainRepository, MainRepository>();

// Entity
builder.Services.AddScoped<GetEntitiesHandler>();
builder.Services.AddScoped<SessionHandler>();
builder.Services.AddScoped<CreateDescriptionHandler>();
builder.Services.AddScoped<AddEntityHandler>();
builder.Services.AddScoped<UpdateItemHandler>();
builder.Services.AddScoped<IEntityRepository, EntityRepository>();

var app = builder.Build();

app.MapGet("/api/entities/{id:guid}", 
    async (Guid id, GetEntitiesHandler entityHandler, CancellationToken ct) 
    => await entityHandler.GetDetailedEntityAsync(id, ct));

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

app.MapGet("/api/tasks/{id:guid}",
    async (Guid id, GetItemsHandler handler, CancellationToken ct)
        => await handler.GetTaskAsync(id, ct));
app.MapGet("/api/tasks", async ( GetItemsHandler handler, CancellationToken ct) 
     => await handler.GetTasksAsync(ct));
app.MapPost("/api/tasks", async ( CreateItemDto dto, CreateItemHandler itemHandler, CancellationToken ct) 
    => await itemHandler.AddTaskAsync(dto, ct));
app.MapPatch("/tasks", async (UpdateItemDto dto, UpdateItemHandler handler) =>
{
    await handler.UpdateAsync(dto);
    return Results.Ok();
});

// Description
app.MapPost("/api/descriptions", async ( DescriptionDto dto, CreateDescriptionHandler descriptionHandler, CancellationToken ct) 
    => await descriptionHandler.CreateAsync(dto));

// Session
app.MapPost("/sessions/start", async (SessionDto dto, SessionHandler handler) 
    => await handler.StartSessionAsync(dto));
app.MapPost("/sessions/pause", async (SessionDto dto, SessionHandler handler) 
    => await handler.PauseSessionAsync(dto));
app.Run();
