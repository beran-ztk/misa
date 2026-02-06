using Microsoft.EntityFrameworkCore;
using Misa.Api.Middleware;
using Misa.Api.Services.Features.Items.Features.Scheduler;
using Misa.Api.Services.Features.Items.Features.Sessions;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Domain.Features.Messaging;
using Misa.Infrastructure.Persistence.Context;
using Misa.Infrastructure.Persistence.Repositories;
using Misa.Infrastructure.Services.Time;
using TimeProvider = System.TimeProvider;

namespace Misa.Api.Composition;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterServicesToBaseDI(this IServiceCollection services)
    {
        services.AddApiCore();
        services.AddApiMiddleware();
        services.AddTimeServices();
        services.AddDataAccess();
        services.AddWorkers();
        services.AddRepositories();

        return services;
    }

    private static IServiceCollection AddApiCore(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddControllers();
        return services;
    }

    private static IServiceCollection AddApiMiddleware(this IServiceCollection services)
    {
        services.AddTransient<ExceptionMappingMiddleware>();
        return services;
    }

    private static IServiceCollection AddTimeServices(this IServiceCollection services)
    {
        services.AddSingleton<ITimeProvider, Misa.Infrastructure.Services.Time.TimeProvider>();
        services.AddSingleton<ITimeZoneProvider, TimeZoneProvider>();
        services.AddSingleton<ITimeZoneConverter, TimeZoneConverter>();
        return services;
    }

    private static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<DefaultContext>((sp, options) =>
        {
            options.UseNpgsql(
                "Host=localhost;Port=5432;Database=misa;Username=postgres;Password=meow",
                npgsql =>
                {
                    npgsql.MapEnum<ScheduleMisfirePolicy>();
                    npgsql.MapEnum<ScheduleFrequencyType>();
                    npgsql.MapEnum<ScheduleActionType>();
                    npgsql.MapEnum<SchedulerExecutionStatus>();
                    npgsql.MapEnum<Priority>();
                    npgsql.MapEnum<ChangeType>();
                    npgsql.MapEnum<Workflow>();
                    npgsql.MapEnum<SessionState>();
                    npgsql.MapEnum<SessionEfficiencyType>();
                    npgsql.MapEnum<SessionConcentrationType>();
                    npgsql.MapEnum<TaskCategory>();
                    npgsql.MapEnum<EventType>();
                    npgsql.MapEnum<OutboxEventState>();
                });
        });

        return services;
    }

    private static IServiceCollection AddWorkers(this IServiceCollection services)
    {
        services.AddHostedService<SessionAutostopWorker>();
        services.AddHostedService<SessionPastMaxTimeWorker>();
        services.AddHostedService<SchedulePlanningWorker>();
        services.AddHostedService<ScheduleExecutingWorker>();
        services.AddHostedService<SchedulePublishingWorker>();
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ISchedulerPlanningRepository, SchedulerPlanningRepository>();
        services.AddScoped<ISchedulerExecutingRepository, SchedulerExecutingRepository>();
        services.AddScoped<ISchedulerRepository, SchedulerRepository>();
        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        return services;
    }
}