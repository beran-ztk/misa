using Misa.Contract.Items.Components.Activity;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Application.Mappings;

public static class ItemActivityMappings
{
    public static ItemActivityDto ToDto(this ItemActivity activity)
    {
        return new ItemActivityDto
        {
            Id = activity.Id.Value,
            State = activity.State.ToDto(),
            Priority = activity.Priority.ToDto(),
            DueAt = activity.DueAt,

            Sessions = activity.Sessions.Select(s => s.ToDto()).ToList(),
        };
    }
    public static ActivityPriority ToDomain(this ActivityPriorityDto activityPriorityDto) =>
        activityPriorityDto switch
        {
            ActivityPriorityDto.None => ActivityPriority.None,
            ActivityPriorityDto.Low => ActivityPriority.Low,
            ActivityPriorityDto.Medium => ActivityPriority.Medium,
            ActivityPriorityDto.High => ActivityPriority.High,
            ActivityPriorityDto.Urgent => ActivityPriority.Urgent,
            ActivityPriorityDto.Critical => ActivityPriority.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(activityPriorityDto), activityPriorityDto, null)
        };
    public static ActivityPriorityDto ToDto(this ActivityPriority activityPriority) =>
        activityPriority switch
        {
            ActivityPriority.None     => ActivityPriorityDto.None,
            ActivityPriority.Low      => ActivityPriorityDto.Low,
            ActivityPriority.Medium   => ActivityPriorityDto.Medium,
            ActivityPriority.High     => ActivityPriorityDto.High,
            ActivityPriority.Urgent   => ActivityPriorityDto.Urgent,
            ActivityPriority.Critical => ActivityPriorityDto.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(activityPriority), activityPriority, null)
        };
    
    public static ActivityState ToDomain(this ActivityStateDto dto) =>
        dto switch
        {
            ActivityStateDto.Draft           => ActivityState.Draft,
            ActivityStateDto.Undefined       => ActivityState.Undefined,
            ActivityStateDto.InProgress      => ActivityState.InProgress,
            ActivityStateDto.Active          => ActivityState.Active,
            ActivityStateDto.Paused          => ActivityState.Paused,
            ActivityStateDto.Pending         => ActivityState.Pending,
            ActivityStateDto.WaitForResponse => ActivityState.WaitForResponse,
            ActivityStateDto.Done            => ActivityState.Done,
            ActivityStateDto.Canceled        => ActivityState.Canceled,
            ActivityStateDto.Failed          => ActivityState.Failed,
            ActivityStateDto.Expired         => ActivityState.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
        };

    public static ActivityStateDto ToDto(this ActivityState domain) =>
        domain switch
        {
            ActivityState.Draft           => ActivityStateDto.Draft,
            ActivityState.Undefined       => ActivityStateDto.Undefined,
            ActivityState.InProgress      => ActivityStateDto.InProgress,
            ActivityState.Active          => ActivityStateDto.Active,
            ActivityState.Paused          => ActivityStateDto.Paused,
            ActivityState.Pending         => ActivityStateDto.Pending,
            ActivityState.WaitForResponse => ActivityStateDto.WaitForResponse,
            ActivityState.Done            => ActivityStateDto.Done,
            ActivityState.Canceled        => ActivityStateDto.Canceled,
            ActivityState.Failed          => ActivityStateDto.Failed,
            ActivityState.Expired         => ActivityStateDto.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
}