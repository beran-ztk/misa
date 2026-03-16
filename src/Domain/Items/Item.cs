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
    public ZettelExtension? ZettelExtension { get; private set; }

    
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
            Activity = new ItemActivity(ActivityState.Open, priority, null, null, dueAt),
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
            Activity = new ItemActivity(ActivityState.Open, priority, objective, null, dueAt),
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
            Activity = new ItemActivity(ActivityState.Open, priority, objective, null, dueAt),
            Unit = new Unit(id, arcId)
        };

        return item;
    }
    public static Item CreateTopic(
        ItemId id,
        string ownerId,
        string title,
        DateTimeOffset createdAtUtc,
        
        ItemId? topicId)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Topic,
            title: title,
            description: null,
            createdAtUtc: createdAtUtc)
        {
            Topic = new Topic(id, topicId)
        };

        return item;
    }

    public static Item CreateZettel(
        ItemId id,
        string ownerId,
        string title,
        string? content,
        DateTimeOffset createdAtUtc,
        ItemId topicId)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Zettel,
            title: title,
            description: null,
            createdAtUtc: createdAtUtc)
        {
            ZettelExtension = new ZettelExtension(id, topicId, content)
        };

        return item;
    }

    // Derived Properties
    
    // Mutators
    public void ChangeTitle(string title)
    {
        if (Title == title)
            return;

        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Title, Title, title, null));
        Title = title;
    }

    public void ChangeDescription(string description)
    {
        if (Description == description)
            return;

        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Description, Description, description, null));
        Description = description;
    }
    public void Archive() => IsArchived = true;
    public void Delete() => IsDeleted = true;
}