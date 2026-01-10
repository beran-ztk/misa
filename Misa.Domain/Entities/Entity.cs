using Misa.Contract.Audit;
using Misa.Domain.Audit;
using Misa.Domain.Dictionaries.Items;
using Misa.Domain.Entities.Extensions;
using Misa.Domain.Items;
using Action = Misa.Domain.Audit.Action;

namespace Misa.Domain.Entities;

public class Entity
{
    private Entity () {}

    public Entity(Guid? ownerId, int workflowId)
    {
        OwnerId = ownerId;
        WorkflowId = workflowId;
        
        CreatedAt = DateTimeOffset.UtcNow;
        InteractedAt = DateTimeOffset.UtcNow;
    }
    
    public Guid Id { get; set; }
    public Guid? OwnerId { get; set; }
    public int WorkflowId { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsArchived { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset InteractedAt { get; set; }

    public Workflow Workflow { get;  set; }
    public Item? Item { get; set; }
    
    public ICollection<Description> Descriptions { get; set; } = new List<Description>();
    
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Domain.Audit.Action> Actions { get; set; } = new List<Action>();

    public bool HasActiveSession 
        => Item?.StateId == (int)Dictionaries.Items.ItemStates.Active;
    public bool CanStartSession
        => Item?.StateId 
            is (int)Dictionaries.Items.ItemStates.Draft
            or (int)Dictionaries.Items.ItemStates.Undefined
            or (int)Dictionaries.Items.ItemStates.InProgress
            or (int)Dictionaries.Items.ItemStates.Pending
            or (int)Dictionaries.Items.ItemStates.WaitForResponse;
    public bool HasRunningSession()
    {
        var session = GetLatestSession();
        if (session == null)
            return false;
        return session.StateId == (int)Dictionaries.Audit.SessionState.Running;
    }
    public bool HasPausedSession()
    {
        var session = GetLatestSession();
        if (session == null)
            return false;
        return session.StateId == (int)Dictionaries.Audit.SessionState.Paused;
    }
    public Session? GetLatestSession()
    {
        return Sessions.Count == 0 ? new Session() 
            : Sessions.MaxBy(s => s.CreatedAtUtc);
            // var singleSession = Sessions.OrderByDescending(s => s.CreatedAtUtc).First();
    }
    public Session? GetLatestActiveSession() 
        => Sessions
            .Where(s => 
                s.StateId is (int)Dictionaries.Audit.SessionState.Running 
            or (int)Dictionaries.Audit.SessionState.Paused )
            .MaxBy(s => s.CreatedAtUtc);
    public void EndSession(StopSessionDto dto)
    {
        var latestActiveSession = GetLatestActiveSession();
        if (latestActiveSession == null)
            return;

        Item?.ChangeState((int)ItemStates.InProgress);
        latestActiveSession.StateId = (int)Dictionaries.Audit.SessionState.Completed;
        latestActiveSession.EfficiencyId = dto.Efficiency;
        latestActiveSession.ConcentrationId = dto.Concentration;
        latestActiveSession.Summary = dto.Summary;

        var latestActiveSegment = latestActiveSession.GetLatestActiveSegment();
        if (latestActiveSegment == null)
            return;
        latestActiveSegment.CloseSegment(null, DateTimeOffset.UtcNow);
    }
    public void Interact() => InteractedAt =  DateTimeOffset.UtcNow;
    public void Update() => UpdatedAt = DateTimeOffset.UtcNow;

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
    public void Archive()
    {
        IsArchived = true;
        ArchivedAt = DateTimeOffset.UtcNow;
    }
}