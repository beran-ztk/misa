using Misa.Api.Endpoints;
using Misa.Api.Endpoints.Deadlines;
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

        // Andere Endpoints
        app.MapDeadlineEndpoints();
        ItemDetailEndpoints.Map(app);
        DescriptionEndpoints.Map(app);
        SchedulingEndpoints.Map(app);
        AuthEndpoints.Map(app);
    }

    private static void MapTaskEndpoints(this IEndpointRouteBuilder api)
    {
        CreateTaskEndpoint.Map(api);
        GetTasksEndpoint.Map(api);
    }

    private static void MapDeadlineEndpoints(this WebApplication app)
    {
        UpsertDeadlineEndpoint.Map(app);
        DeleteDeadlineEndpoint.Map(app);
    }
}
