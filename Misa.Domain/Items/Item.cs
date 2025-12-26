using Misa.Domain.Dictionaries.Audit;
using Misa.Domain.Entities;
using Misa.Domain.Main;

namespace Misa.Domain.Items;

public class Item : ChangeEvent
{
    // Für EF Core
    private Item() { }

    public Item(
        Entity entity,
        int stateId,
        int priorityId,
        int categoryId,
        string title)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));

        StateId = stateId;
        PriorityId = priorityId;
        CategoryId = categoryId;
        Title = title ?? throw new ArgumentNullException(nameof(title));
    }
    // Member
    public Guid EntityId { get; set; }
    public int StateId { get; private set; }
    public int PriorityId { get; private set; }
    public int CategoryId { get; private set; }
    public string Title { get; private set; }
    
    // Modelle
    public Entity Entity { get; set; }
    public State State { get; private set; }
    public Priority Priority { get; private set; }
    public Category Category { get; private set; }

    public void ChangeState(int? optionalNewValue, ref bool changed,  string? reason = null)
    {
        if (StateId == optionalNewValue || optionalNewValue is null)
            return;

        var newValue = Convert.ToInt32(optionalNewValue);
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.State,
            OldValue: StateId.ToString(),
            NewValue: newValue.ToString(),
            Reason: reason
        ));
        StateId = newValue;
        
        changed = true;
    }
    public void ChangeState(int? optionalNewValue,  string? reason = null)
    {
        if (StateId == optionalNewValue || optionalNewValue is null)
            return;

        var newValue = Convert.ToInt32(optionalNewValue);
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.State,
            OldValue: StateId.ToString(),
            NewValue: newValue.ToString(),
            Reason: reason
        ));
        StateId = newValue;
    }
    public void StartSession(ref bool hasBeenChanged) => ChangeState((int)Dictionaries.Items.ItemStates.Active, ref hasBeenChanged);
    public void ChangePriority(int? optionalNewValue, ref bool changed, string? reason = null)
    {
        if (PriorityId == optionalNewValue || optionalNewValue is null)
            return;

        var newValue = Convert.ToInt32(optionalNewValue);
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.Priority,
            OldValue: Priority.ToString(),
            NewValue: newValue.ToString(),
            Reason: reason
        ));
        PriorityId = newValue;
        
        changed = true;
    }
    public void ChangeCategory(int? optionalNewValue, ref bool changed,  string? reason = null)
    {
        if (PriorityId == optionalNewValue || optionalNewValue is null)
            return;

        var newValue = Convert.ToInt32(optionalNewValue);
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.Category,
            OldValue: Category.ToString(),
            NewValue: newValue.ToString(),
            Reason: reason
        ));
        CategoryId = newValue;
        
        changed = true;
    }
    public void Rename(string? optionalNewTitle, ref bool changed,  string? reason = null)
    {
        if (Title == optionalNewTitle || string.IsNullOrWhiteSpace(optionalNewTitle))
        {
            return;
        }

        var newValue = Convert.ToString(optionalNewTitle).Trim();
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.Title,
            OldValue: Title,
            NewValue: newValue,
            Reason: reason
        ));
        Title = newValue;
        
        changed = true;
    }
}