using Misa.Infrastructure.Data;
using Misa.Infrastructure.Items;
using Misa.Application.Items.Repositories;
using Misa.Contract.Items;
using Microsoft.EntityFrameworkCore;
using Misa.Application.Entities.Add;
using Misa.Application.Entities.Get;
using Misa.Application.Entities.Repositories;
using Misa.Application.Items.Add;
using Misa.Contract.Entities;
using Misa.Infrastructure.Entities;

const string connectionString =
    "Host=localhost;Port=5432;Database=misa;Username=postgres;Password=meow";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MisaDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<CreateItemHandler>();
builder.Services.AddScoped<CreateItemHandler>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// Entity
builder.Services.AddScoped<GetEntitiesHandler>();
builder.Services.AddScoped<AddEntityHandler>();
builder.Services.AddScoped<IEntityRepository, EntityRepository>();

var app = builder.Build();


app.MapGet("/api/entities/get", async (
    GetEntitiesHandler handler,
    CancellationToken ct) =>
{
    var entities = await handler.GetAllAsync(ct);
    return entities;
});

app.MapPost("/api/entities/add", async (
    CreateEntityDto dto,
    AddEntityHandler handler,
    CancellationToken ct) =>
{
    var entity = dto.Transform();
    await handler.AddAsync(entity, ct);
    return Results.Ok();
});

// Tasks
app.MapPost("/api/tasks", async ( CreateItemDto dto, CreateItemHandler itemHandler, CancellationToken ct) 
    => await itemHandler.AddTaskAsync(dto, ct));

app.MapGet("/api/tasks", async (
    GetEntitiesHandler handler,
    CancellationToken ct) =>
{
});

app.Run();
