using Misa.Api.Endpoints.Features.Authentication;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Base;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Extensions;
using Misa.Api.Endpoints.Features.Entities.Extensions.Items.Features;
using Misa.Api.Endpoints.Features.Entities.Features;
using Misa.Api.Middleware;

namespace Misa.Api.Composition;

public static class EndpointRegistration
{
    public static WebApplication MapAllEndpoints(this WebApplication app)
    {
        app.MapControllers();

        app.MapHub<EventHub>("/hubs/updates");

        TaskEndpoints.Map(app);
        ItemDetailEndpoints.Map(app);
        DescriptionEndpoints.Map(app);
        SchedulingEndpoints.Map(app);
        AuthEndpoints.Map(app);

        return app;
    }
}