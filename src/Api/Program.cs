using Misa.Api.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterServices();
builder.Host.RegisterHandlersToWolverine();

var app = builder.Build();

app.MapAllEndpoints();

app.Run();