using Misa.Api.Composition;
using Misa.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterServicesToBaseDI();
builder.Host.RegisterHandlersToWolverine();

var app = builder.Build();

app.UseMiddleware<ExceptionMappingMiddleware>();
app.MapAllEndpoints();

app.Run();