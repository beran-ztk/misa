using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Misa.Api.Services.Auth;
using Misa.Api.Services.Features.Items.Features.Scheduler;
using Misa.Api.Services.Features.Items.Features.Sessions;
using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Tasks;
using Misa.Infrastructure.Auth;
using Misa.Infrastructure.Persistence.Context;
using Misa.Infrastructure.Persistence.Repositories;
using Misa.Infrastructure.Services.Ids;
using Misa.Infrastructure.Services.Time;

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
        services.AddScoped<IIdentityAuthStore, IdentityAuthStore>();
        
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddDbContext<IdentityContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("User")));

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        
        services
            .AddIdentityCore<User>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireNonAlphanumeric = false;
                opt.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<IdentityContext>()
            .AddSignInManager();
        
        
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                var jwt = configuration.GetSection("Jwt");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt["Key"]!))
                };
            });

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
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ISchedulerPlanningRepository, SchedulerPlanningRepository>();
        services.AddScoped<ISchedulerExecutingRepository, SchedulerExecutingRepository>();
        services.AddScoped<ISchedulerRepository, SchedulerRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
    }
    private static void AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MisaContext>((_, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Misa"),
                npgsql =>
                {
                    npgsql.MapEnum<ScheduleMisfirePolicy>();
                    npgsql.MapEnum<ScheduleFrequencyType>();
                    npgsql.MapEnum<ScheduleActionType>();
                    npgsql.MapEnum<ScheduleExecutionStatus>();
                    npgsql.MapEnum<ActivityPriority>();
                    npgsql.MapEnum<ChangeType>();
                    npgsql.MapEnum<Workflow>();
                    npgsql.MapEnum<SessionState>();
                    npgsql.MapEnum<SessionEfficiencyType>();
                    npgsql.MapEnum<SessionConcentrationType>();
                    npgsql.MapEnum<TaskCategory>();
                    npgsql.MapEnum<JournalSystemType>();
                });
        });
    }
}