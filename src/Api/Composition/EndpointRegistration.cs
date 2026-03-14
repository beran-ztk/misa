using Misa.Api.Endpoints.Authentication;
using Misa.Api.Endpoints.Chronicle;
using Misa.Api.Endpoints.Inspector;
using Misa.Api.Endpoints.Relations;
using Misa.Api.Endpoints.Schedules;
using Misa.Api.Endpoints.Schola;
using Misa.Api.Endpoints.Sessions;
using Misa.Api.Endpoints.Tasks;
using Misa.Api.Endpoints.Zettelkasten;
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
        EntryEndpoints.Map(app);
        ChronicleEndpoints.Map(app);
        ScholaEndpoints.Map(app);
        TopicEndpoints.Map(app);
        ZettelEndpoints.Map(app);
        RelationEndpoints.Map(app);
        app.MapScheduleEndpoints();
        app.MapAuthenticationEndpoints();
        app.MapInspectorEndpoints();
    }
    private static void MapScheduleEndpoints(this IEndpointRouteBuilder api)
    {
        CreateScheduleEndpoint.Map(api);
        GetSchedulesEndpoint.Map(api);
        UpdateScheduleEndpoint.Map(api);
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
