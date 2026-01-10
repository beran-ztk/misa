using System.Security.AccessControl;
using Misa.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Entities;
using Misa.Domain.Entities.Extensions;

namespace Misa.Infrastructure.Data;

public class MisaDbContext : DbContext
{
    public MisaDbContext(DbContextOptions<MisaDbContext> options) 
        : base(options) {}

    public DbSet<Misa.Domain.Entities.Entity> Entities { get; set; } = null;
    public DbSet<Misa.Domain.Items.Item> Items { get; set; } = null;
    public DbSet<Misa.Domain.Items.State> States { get; set; } = null;
    public DbSet<Misa.Domain.Items.Priority> Priorities { get; set; } = null;
    public DbSet<Misa.Domain.Items.Category> Categories { get; set; } = null;
    public DbSet<Misa.Domain.Entities.Workflow> Workflows { get; set; } = null;
    public DbSet<Description> Descriptions { get; set; } = null;
    
    public DbSet<Misa.Domain.Audit.Session> Sessions { get; set; } = null;
    public DbSet<Misa.Domain.Audit.SessionSegment> SessionSegments { get; set; } = null;
    public DbSet<Misa.Domain.Audit.SessionStates> SessionStates { get; set; } = null;
    public DbSet<Misa.Domain.Audit.SessionEfficiencyType> EfficiencyTypes { get; set; } = null;
    public DbSet<Misa.Domain.Audit.SessionConcentrationType> ConcentrationTypes { get; set; } = null;
    public DbSet<Misa.Domain.Audit.Action> Actions { get; set; } = null;
    public DbSet<Misa.Domain.Audit.ActionType> ActionTypes { get; set; } = null;
    public DbSet<Misa.Domain.Scheduling.ScheduledDeadline> Deadlines { get; set; } = null;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MisaDbContext).Assembly);
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
            Actions.Add(new Domain.Audit.Action
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