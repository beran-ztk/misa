using Misa.Contract.Items.Components.Activity.Sessions;

namespace Misa.Contract.Items.Components.Activity;

public sealed record ItemActivityDto
{
    public required Guid Id { get; init; }
    public required ActivityStateDto State { get; init; }
    public required ActivityPriorityDto Priority { get; init; }
    public DateTimeOffset? DueAt { get; init; }

    public required ICollection<SessionDto> Sessions { get; init; }
}