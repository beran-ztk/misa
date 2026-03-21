using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Audits.Changes;
using Misa.Domain.Items.Components.Chronicle.Journals;
using Misa.Domain.Items.Components.Schedules;
using Misa.Domain.Items.Components.Schola;
using Misa.Domain.Items.Components.Tasks;
using Misa.Domain.Items.Components.Zettelkasten;

namespace Misa.Domain.Items;
public readonly record struct ItemId(Guid Value);

public sealed class Item : DomainEventEntity
{
    private Item() { } // EF

    private Item(
        ItemId id,
        string ownerId,
        Workflow workflow,
        string title,
        string? description,
        DateTimeOffset createdAtUtc)
    {
        if (id.Equals(default))
            throw new DomainValidationException("id", "id_required", "Id is required.");

        if (string.IsNullOrWhiteSpace(ownerId))
            throw new DomainValidationException("ownerId", "owner_required", "OwnerId is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException("title", "title_required", "Title is required.");

        Id = id;
        OwnerId = ownerId;
        Workflow = workflow;

        Title = title;
        Description = description;

        CreatedAt = createdAtUtc;
    }
    
    // Fields + Properties
    public ItemId Id { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public Workflow Workflow { get; init; }
    
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public bool IsDeleted { get; private set; }
    public bool IsArchived { get; private set; }
    
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; private set; }
    
    // Components-Standard
    public ICollection<AuditChange> Changes { get; init; } = new List<AuditChange>();
    
    // Components-Activity
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ItemActivity? Activity { get; private set; }
    public TaskExtension? TaskExtension { get; private set; }
    public ScheduleExtension? ScheduleExtension { get; private set; }
    public JournalExtension? JournalExtension { get; private set; }
    public Arc? Arc { get; private set; }
    public Unit? Unit { get; private set; }
    public Topic? Topic { get; private set; }
    public Zettel? ZettelExtension { get; private set; }
    public KnowledgeIndex? KnowledgeIndex { get; private set; }

    
    // Behaviours
    public static Item CreateTask(
        ItemId id,
        string ownerId,
        string title,
        string? description,
        TaskCategory category,
        DateTimeOffset createdAtUtc,
        
        ActivityPriority priority,
        DateTimeOffset? dueAt)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Task,
            title: title,
            description: description,
            createdAtUtc: createdAtUtc)
        {
            Activity = new ItemActivity(id, ActivityState.Open, priority, null, null, dueAt),
            TaskExtension = new TaskExtension(id, category)
        };

        return item;
    }
    public static Item CreateSchedule(
        ItemId id,
        string ownerId,
        string title,
        string? description,
        DateTimeOffset createdAtUtc,
        ScheduleExtension scheduleExtension)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Schedule,
            title: title,
            description: description,
            createdAtUtc: createdAtUtc)
        {
            ScheduleExtension = scheduleExtension
        };

        return item;
    }
    
    public static Item CreateJournal(
        ItemId id,
        string ownerId,
        string title,
        string? description,
        DateTimeOffset createdAtUtc,
        JournalExtension journalExtension)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Journal,
            title: title,
            description: description,
            createdAtUtc: createdAtUtc)
        {
            JournalExtension = journalExtension
        };

        return item;
    }
    
    public static Item CreateArc(
        ItemId id,
        string ownerId,
        string title,
        string? description,
        DateTimeOffset createdAtUtc,
        
        ActivityPriority priority,
        string? objective,
        DateTimeOffset? dueAt)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Arc,
            title: title,
            description: description,
            createdAtUtc: createdAtUtc)
        {
            Activity = new ItemActivity(id, ActivityState.Open, priority, objective, null, dueAt),
            Arc = new Arc(id)
        };

        return item;
    }
    public static Item CreateUnit(
        ItemId id,
        string ownerId,
        string title,
        string? description,
        DateTimeOffset createdAtUtc,
        
        ActivityPriority priority,
        string? objective,
        DateTimeOffset? dueAt,
        
        ItemId? arcId)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Unit,
            title: title,
            description: description,
            createdAtUtc: createdAtUtc)
        {
            Activity = new ItemActivity(id, ActivityState.Open, priority, objective, null, dueAt),
            Unit = new Unit(id, arcId)
        };

        return item;
    }
    public static Item CreateTopic(
        ItemId id,
        string ownerId,
        string title,
        DateTimeOffset createdAtUtc,

        ItemId? parentId)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Topic,
            title: title,
            description: null,
            createdAtUtc: createdAtUtc)
        {
            Topic = new Topic(id),
            KnowledgeIndex = new KnowledgeIndex(id, parentId)
        };

        return item;
    }

    public static Item CreateZettel(
        ItemId id,
        string ownerId,
        string title,
        DateTimeOffset createdAtUtc,
        ItemId? parentId)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Zettel,
            title: title,
            description: null,
            createdAtUtc: createdAtUtc)
        {
            ZettelExtension = new Zettel(id),
            KnowledgeIndex = new KnowledgeIndex(id, parentId)
        };

        return item;
    }

    // Derived Properties
    
    // Mutators

    /// <summary>Updates ModifiedAt. Called by all domain mutators — not intended for application-layer use.</summary>
    internal void Touch(DateTimeOffset nowUtc) => ModifiedAt = nowUtc;

    public void ChangeTitle(string title, DateTimeOffset nowUtc)
    {
        if (Title == title)
            return;

        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Title, Title, title, null));
        Title = title;
        Touch(nowUtc);
    }

    public void ChangeDescription(string description, DateTimeOffset nowUtc)
    {
        if (Description == description)
            return;

        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Description, Description, description, null));
        Description = description;
        Touch(nowUtc);
    }

    public void Archive(DateTimeOffset nowUtc) { IsArchived = true; Touch(nowUtc); }
    public void Delete(DateTimeOffset nowUtc)  { IsDeleted  = true; Touch(nowUtc); }
    public void Restore(DateTimeOffset nowUtc) { IsArchived = false; IsDeleted = false; Touch(nowUtc); }

    // ── Child-extension proxies ───────────────────────────────────────────────
    // Owned child entities without an Item back-navigation property route their
    // mutations through here so ModifiedAt is updated consistently.

    public void ChangeJournalOccurredAt(DateTimeOffset occurredAt, DateTimeOffset nowUtc)
    {
        JournalExtension!.ChangeOccurredAt(occurredAt);
        Touch(nowUtc);
    }

    public void ChangeTaskCategory(TaskCategory category, DateTimeOffset nowUtc)
    {
        TaskExtension!.ChangeCategory(category);
        Touch(nowUtc);
    }

    public void ChangeZettelContent(string? content, DateTimeOffset nowUtc)
    {
        ZettelExtension!.ChangeContent(content);
        Touch(nowUtc);
    }

    public void ChangeScheduleMisfirePolicy(ScheduleMisfirePolicy policy, DateTimeOffset nowUtc)
    {
        ScheduleExtension!.ChangeMisfirePolicy(policy);
        Touch(nowUtc);
    }

    public void ChangeScheduleLookaheadLimit(int limit, DateTimeOffset nowUtc)
    {
        ScheduleExtension!.ChangeLookaheadLimit(limit);
        Touch(nowUtc);
    }

    public void ChangeScheduleOccurrenceCountLimit(int? limit, DateTimeOffset nowUtc)
    {
        ScheduleExtension!.ChangeOccurrenceCountLimit(limit);
        Touch(nowUtc);
    }

    public void ChangeScheduleStartTime(TimeOnly? startTime, DateTimeOffset nowUtc)
    {
        ScheduleExtension!.ChangeStartTime(startTime);
        Touch(nowUtc);
    }

    public void ChangeScheduleEndTime(TimeOnly? endTime, DateTimeOffset nowUtc)
    {
        ScheduleExtension!.ChangeEndTime(endTime);
        Touch(nowUtc);
    }

    public void ChangeScheduleActiveUntil(DateTimeOffset? activeUntilUtc, DateTimeOffset nowUtc)
    {
        ScheduleExtension!.ChangeActiveUntil(activeUntilUtc);
        Touch(nowUtc);
    }
}