using Misa.Contract.Items.Components.Activity;

namespace Misa.Contract.Items.Components.Schola;

public sealed record UnitDto
{
    public required ItemDto Item { get; init; }
    public required ItemActivityDto Activity { get; init; }
    
    public required Guid? ArcId { get; init; }
}