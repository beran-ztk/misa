using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Features.Audit;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Schedules;
using Misa.Domain.Items.Components.Tasks;
using Misa.Domain.Shared.DomainEvents;

namespace Misa.Infrastructure.Persistence.Context;

public class MisaContext(DbContextOptions<MisaContext> options, ITimeProvider timeProvider, IIdGenerator idGenerator) : DbContext(options)
{
    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<TaskExtension> Tasks { get; set; } = null!;
    
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<SessionSegment> SessionSegments { get; set; } = null!;
    public DbSet<AuditChange> AuditChanges { get; set; } = null!;

    public DbSet<ScheduleExtension> Schedulers { get; set; } = null!;
    public DbSet<ScheduleExecutionLog> SchedulerExecutionLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MisaContext).Assembly);
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
            AuditChanges.Add(
                new AuditChange(
                    idGenerator.New(), 
                    ev.EntityId,
                    ev.ChangeType,
                    ev.OldValue,
                    ev.NewValue,
                    null,
                    timeProvider.UtcNow
                )
            );
        }

        foreach (var e in tracked)
        {
            e.ClearDomainEvents();
        }
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