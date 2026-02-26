using Misa.Contract.Items.Components.Activity;

namespace Misa.Contract.Items.Components.Tasks;
public sealed record TaskDto
{
    public required ItemDto Item { get; init; }
    public required ItemActivityDto Activity { get; init; }
    public required TaskCategoryDto Category { get; init; }
}