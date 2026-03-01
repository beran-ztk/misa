using Misa.Contract.Items.Components.Activity;

namespace Misa.Contract.Items.Components.Schola;

public sealed record CreateArcRequest(
    string Title,
    string? Description,
    ActivityPriorityDto ActivityPriorityDto,
    string? Objective
);