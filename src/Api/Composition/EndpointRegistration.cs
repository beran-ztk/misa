using Misa.Api.Endpoints.Authentication;
using Misa.Api.Endpoints.Inspector;
using Misa.Api.Endpoints.Schedules;
using Misa.Api.Endpoints.Sessions;
using Misa.Api.Endpoints.Tasks;
using Misa.Api.Middleware;

namespace Misa.Api.Composition;

public static class EndpointRegistration
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<EventHub>("/hubs/updates");

        app.UseMiddleware<HttpExceptionMiddleware>();

        TaskEndpoints.Map(app);
        SessionEndpoints.Map(app);
        app.MapScheduleEndpoints();
        app.MapAuthenticationEndpoints();
        app.MapInspectorEndpoints();
    }
    private static void MapScheduleEndpoints(this IEndpointRouteBuilder api)
    {
        CreateScheduleEndpoint.Map(api);
        GetSchedulesEndpoint.Map(api);
    }
    private static void MapAuthenticationEndpoints(this IEndpointRouteBuilder api)
    {
        RegisterEndpoint.Map(api);
        LoginEndpoint.Map(api);
    }
    private static void MapInspectorEndpoints(this IEndpointRouteBuilder api)
    {
        GetSessionDetailsEndpoint.Map(api);
    }
}
