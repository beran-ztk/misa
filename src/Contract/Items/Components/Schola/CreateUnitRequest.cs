using Misa.Contract.Items.Components.Activity;

namespace Misa.Contract.Items.Components.Schola;

public sealed record CreateUnitRequest(
    string Title,
    string? Description,
    ActivityPriorityDto ActivityPriorityDto,
    string? Objective
);