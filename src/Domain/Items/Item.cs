using Misa.Domain.Exceptions;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Features;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Tasks;
using Misa.Domain.Shared.DomainEvents;

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
        string description,
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

    public static Item CreateTask(
        ItemId id,
        string ownerId,
        string title,
        string description,
        TaskCategory category,
        DateTimeOffset createdAtUtc)
    {
        var item = new Item(
            id: id,
            ownerId: ownerId,
            workflow: Workflow.Task,
            title: title,
            description: description,
            createdAtUtc: createdAtUtc);

        item.TaskExtension = new TaskExtension(item.Id, category);

        return item;
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
    public ICollection<Description> Descriptions { get; init; } = new List<Description>();
    
    // Components-Activity
    public ItemActivity? Activity { get; private set; }
    public TaskExtension? TaskExtension { get; private set; }

    // Derived Properties
    
    // Mutators
}