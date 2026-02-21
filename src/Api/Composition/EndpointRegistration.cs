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

        // Group für Result-basierte Endpoints
        var api = app.MapGroup("");
        api.AddEndpointFilter<ResultExceptionFilter>();

        api.MapTaskEndpoints();
        api.MapSessionEndpoints();
        api.MapScheduleEndpoints();
        api.MapAuthenticationEndpoints();
        api.MapInspectorEndpoints();
    }

    private static void MapTaskEndpoints(this IEndpointRouteBuilder api)
    {
        CreateTaskEndpoint.Map(api);
        GetTasksEndpoint.Map(api);
    }

    private static void MapSessionEndpoints(this IEndpointRouteBuilder api)
    {
        StartSessionEndpoint.Map(api);
        PauseSessionEndpoint.Map(api);
        ContinueSessionEndpoint.Map(api);
        StopSessionEndpoint.Map(api);
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
