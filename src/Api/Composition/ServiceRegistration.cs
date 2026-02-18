using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Misa.Api.Services.Features.Items.Features.Scheduler;
using Misa.Api.Services.Features.Items.Features.Sessions;
using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Domain.Features.Messaging;
using Misa.Infrastructure.Auth;
using Misa.Infrastructure.Persistence.Context;
using Misa.Infrastructure.Persistence.Repositories;
using Misa.Infrastructure.Services.Ids;
using Misa.Infrastructure.Services.Time;
using User = Misa.Infrastructure.Auth.User;

namespace Misa.Api.Composition;

public static class ServiceRegistration
{
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApiCore();
        services.AddCoreServices();
        services.AddDataAccess(configuration);
        services.AddWorkers();
        services.AddRepositories();
        services.AddIdentity(configuration);
    }

    private static void AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization();
        services.AddAuthentication();
        
        services.AddDbContext<AuthContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default")));

        services
            .AddIdentityCore<User>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireNonAlphanumeric = false;
                opt.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AuthContext>()
            .AddSignInManager();
        
        services.AddScoped<IIdentityAuthStore, IdentityAuthStore>();
    }
    private static void AddApiCore(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddControllers();
    }
    

    private static void AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<ITimeProvider, Misa.Infrastructure.Services.Time.TimeProvider>();
        services.AddSingleton<ITimeZoneProvider, TimeZoneProvider>();
        services.AddSingleton<ITimeZoneConverter, TimeZoneConverter>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
    }

    private static void AddWorkers(this IServiceCollection services)
    {
        services.AddHostedService<SessionAutostopWorker>();
        services.AddHostedService<SessionPastMaxTimeWorker>();
        services.AddHostedService<SchedulePlanningWorker>();
        services.AddHostedService<ScheduleExecutingWorker>();
        services.AddHostedService<SchedulePublishingWorker>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ISchedulerPlanningRepository, SchedulerPlanningRepository>();
        services.AddScoped<ISchedulerExecutingRepository, SchedulerExecutingRepository>();
        services.AddScoped<ISchedulerRepository, SchedulerRepository>();
        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        services.AddScoped<IDeadlineRepository, DeadlineRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IChronicleRepository, ChronicleRepository>();
    }
    private static void AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DefaultContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
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
                    npgsql.MapEnum<JournalSystemType>();
                });
        });
    }
}