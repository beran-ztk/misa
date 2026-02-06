using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Domain.Features.Entities.Features;
using Misa.Domain.Features.Messaging;
using Misa.Domain.Features.Users;
using Misa.Domain.Shared.DomainEvents;
using Task = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;

namespace Misa.Infrastructure.Persistence.Context;

public class DefaultContext(DbContextOptions<DefaultContext> options, ITimeProvider timeProvider) : DbContext(options)
{
    public DbSet<Entity> Entities { get; set; } = null!;
    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<Task> Tasks { get; set; } = null!;
    public DbSet<State> States { get; set; } = null!;
    public DbSet<Description> Descriptions { get; set; } = null!;
    
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<SessionSegment> SessionSegments { get; set; } = null!;
    public DbSet<AuditChange> AuditChanges { get; set; } = null!;

    public DbSet<Scheduler> Schedulers { get; set; } = null!;
    public DbSet<SchedulerExecutionLog> SchedulerExecutionLogs { get; set; } = null!;
    public DbSet<Outbox> Outbox { get; set; } = null!;
    public DbSet<User> User { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DefaultContext).Assembly);
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