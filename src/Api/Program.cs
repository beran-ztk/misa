using Misa.Api.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterServices(builder.Configuration);
builder.Host.RegisterHandlersToWolverine();

var app = builder.Build();

app.MapAllEndpoints();

app.Run();