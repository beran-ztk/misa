using Microsoft.EntityFrameworkCore;
using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Features.Actions;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Domain.Features.Entities.Features.Descriptions;
using Action = Misa.Domain.Features.Actions.Action;

namespace Misa.Infrastructure.Persistence.Context;

public class DefaultContext : DbContext
{
    public DefaultContext(DbContextOptions<DefaultContext> options) 
        : base(options) {}

    public DbSet<Entity> Entities { get; set; } = null;
    public DbSet<Item> Items { get; set; } = null;
    public DbSet<State> States { get; set; } = null;
    public DbSet<Priority> Priorities { get; set; } = null;
    public DbSet<Category> Categories { get; set; } = null;
    public DbSet<Workflow> Workflows { get; set; } = null;
    public DbSet<Description> Descriptions { get; set; } = null;
    
    public DbSet<Session> Sessions { get; set; } = null;
    public DbSet<SessionSegment> SessionSegments { get; set; } = null;
    public DbSet<SessionStates> SessionStates { get; set; } = null;
    public DbSet<SessionEfficiencyType> EfficiencyTypes { get; set; } = null;
    public DbSet<SessionConcentrationType> ConcentrationTypes { get; set; } = null;
    public DbSet<Action> Actions { get; set; } = null;
    public DbSet<ActionType> ActionTypes { get; set; } = null;
    public DbSet<ScheduledDeadline> Deadlines { get; set; } = null;
    
    public DbSet<Scheduler> Scheduler { get; set; }
    public DbSet<SchedulerExecutionLog> SchedulerExecutionLog { get; set; }
    public DbSet<SchedulerFrequencyType> SchedulerFrequencyType { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DefaultContext).Assembly);
        modelBuilder.HasPostgresEnum("scheduler_misfire_policy", ["Skip", "RunOnce", "Catchup"]);
    }

    private void WriteAuditFromDomainEvents()
    {
        var tracked = ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var events = tracked
            .SelectMany(e => e.DomainEvents)
            .OfType<PropertyChangedEvent>()
            .ToList();

        foreach (var ev in events)
        {
            Actions.Add(new Action
            {
                EntityId = ev.EntityId,
                TypeId = ev.ActionType,
                ValueBefore = ev.OldValue,
                ValueAfter = ev.NewValue,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        
        foreach (var e in tracked)
            e.ClearDomainEvents();
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        WriteAuditFromDomainEvents();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        WriteAuditFromDomainEvents();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

}