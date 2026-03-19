using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Contract.Items.Components.Schedules;

/// <summary>
/// Typed payload for a CreateTask schedule action.
/// Serialized to JSON and stored in <see cref="CreateScheduleRequest.Payload"/>.
/// </summary>
public sealed record CreateTaskSchedulePayload(
    string              Title,
    string?             Description,
    TaskCategoryDto     Category,
    ActivityPriorityDto Priority);
