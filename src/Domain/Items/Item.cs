using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Audits.Changes;
using Misa.Domain.Items.Components.Journal;
using Misa.Domain.Items.Components.Schedules;
using Misa.Domain.Items.Components.Tasks;
using Misa.Domain.Items.Components.Zettelkasten;

namespace Misa.Domain.Items;
public readonly record struct ItemId(Guid Value);

public sealed class Item : DomainEventEntity
{
    private Item() { } // EF

    private Item(
        Workflow workflow,
        string title,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException("title", "title_required", "Title is required.");

        Id = new ItemId(Guid.NewGuid());
        Workflow = workflow;

        Title = title;
        Description = description;

        CreatedAt = DateTimeOffset.UtcNow;
    }
    
    // Fields + Properties
    public ItemId Id { get; init; }
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
    public Schedule? ScheduleExtension { get; private set; }
    public JournalExtension? JournalExtension { get; private set; }
    public Topic? Topic { get; private set; }
    public Zettel? ZettelExtension { get; private set; }
    public KnowledgeIndex? KnowledgeIndex { get; private set; }

    
    // Behaviours
    public static Item CreateTask(
        string title,
        string? description,
        TaskCategory category,
        ActivityPriority priority,
        DateTimeOffset? dueAt)
    {
        var item = new Item(Workflow.Task, title, description);
        item.Activity = new ItemActivity(item.Id, ActivityState.Open, priority, null, null, dueAt);
        item.TaskExtension = new TaskExtension(item.Id, category);
        return item;
    }

    public static Item CreateJournal(
        string title,
        string? description,
        JournalExtension journalExtension)
    {
        var item = new Item(Workflow.Journal, title, description);
        item.JournalExtension = journalExtension;
        return item;
    }

    public static Item CreateTopic(
        string title,
        ItemId? parentId)
    {
        var item = new Item(Workflow.Topic, title, null);
        item.Topic = new Topic(item.Id);
        item.KnowledgeIndex = new KnowledgeIndex(item.Id, parentId);
        return item;
    }

    public static Item CreateZettel(
        string title,
        ItemId? parentId)
    {
        var item = new Item(Workflow.Zettel, title, null);
        item.ZettelExtension = new Zettel(item.Id);
        item.KnowledgeIndex = new KnowledgeIndex(item.Id, parentId);
        return item;
    }

    // Derived Properties
    
    // Mutators

    private void Touch() => ModifiedAt = DateTimeOffset.UtcNow;

    public void ChangeTitle(string title)
    {
        if (Title == title)
            return;

        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Title, Title, title, null));
        Title = title;
        Touch();
    }

    public void ChangeDescription(string description)
    {
        if (Description == description)
            return;

        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Description, Description, description, null));
        Description = description;
        Touch();
    }

    public void Archive() { IsArchived = true; Touch(); }
    public void Delete()  { IsDeleted  = true; Touch(); }
    public void Restore() { IsArchived = false; IsDeleted = false; Touch(); }

    // ── Child-extension proxies ───────────────────────────────────────────────
    // Owned child entities without an Item back-navigation property route their
    // mutations through here so ModifiedAt is updated consistently.

    public void ChangeJournalOccurredAt(DateTimeOffset occurredAt)
    {
        JournalExtension!.ChangeOccurredAt(occurredAt);
        Touch();
    }

    public void ChangeTaskCategory(TaskCategory category)
    {
        TaskExtension!.ChangeCategory(category);
        Touch();
    }

    public void ChangeZettelContent(string? content)
    {
        ZettelExtension!.ChangeContent(content);
        Touch();
    }

    public void SetKnowledgeIndexExpanded(bool isExpanded)
    {
        KnowledgeIndex!.SetExpanded(isExpanded);
    }

    public void ReparentKnowledgeIndex(ItemId newParentId)
    {
        KnowledgeIndex!.SetParentId(newParentId);
        Touch();
    }
}