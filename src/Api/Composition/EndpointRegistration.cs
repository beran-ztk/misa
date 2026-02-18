using Misa.Api.Endpoints;
using Misa.Api.Endpoints.Deadlines;
using Misa.Api.Endpoints.Descriptions;
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
        api.MapDeadlineEndpoints();
        api.MapSessionEndpoints();
        api.MapScheduleEndpoints();
        api.MapDescriptionEndpoints();
        
        ItemDetailEndpoints.Map(app);
        AuthEndpoints.Map(app);
    }

    private static void MapTaskEndpoints(this IEndpointRouteBuilder api)
    {
        CreateTaskEndpoint.Map(api);
        GetTasksEndpoint.Map(api);
    }

    private static void MapDeadlineEndpoints(this IEndpointRouteBuilder api)
    {
        UpsertDeadlineEndpoint.Map(api);
        DeleteDeadlineEndpoint.Map(api);
    }
    private static void MapSessionEndpoints(this IEndpointRouteBuilder api)
    {
        StartSessionEndpoint.Map(api);
        PauseSessionEndpoint.Map(api);
        StopSessionEndpoint.Map(api);
    }
    private static void MapScheduleEndpoints(this IEndpointRouteBuilder api)
    {
        CreateScheduleEndpoint.Map(api);
        GetSchedulesEndpoint.Map(api);
    }
    private static void MapDescriptionEndpoints(this IEndpointRouteBuilder api)
    {
        CreateDescriptionEndpoint.Map(api);
        UpdateDescriptionEndpoint.Map(api);
        DeleteDescriptionEndpoint.Map(api);
    }
}
