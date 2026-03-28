using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Core.Features.Items.Schedules;
public record SchedulePlanningCommand;
public class SchedulePlanningHandler(SchedulerPlanningRepository repository)
{
    private static readonly HashSet<ScheduleFrequencyType> AllowedFrequencies =
    [
        ScheduleFrequencyType.Minutes,
        ScheduleFrequencyType.Hours,
        ScheduleFrequencyType.Days
    ];
    public async Task HandleAsync(SchedulePlanningCommand command, CancellationToken stoppingToken)
    {
        var schedules = await repository.GetActiveSchedulesAsync(stoppingToken);

        foreach (var schedule in schedules)
        {
            if (schedule.ScheduleExtension is null) continue;
            if (!AllowedFrequencies.Contains(schedule.ScheduleExtension.ScheduleFrequencyType)) continue;

            
            var localNow = DateTimeOffset.Now;

            var currentLookaheadCount = await repository.GetExecutionCountPlannedAheadAsync(schedule.ScheduleExtension.Id.Value, stoppingToken);
            
            var maxLocalLookaheadTime = schedule.ScheduleExtension.ScheduleFrequencyType switch
            {
                ScheduleFrequencyType.Minutes => localNow.AddDays(3),
                ScheduleFrequencyType.Hours => localNow.AddDays(180),
                ScheduleFrequencyType.Days => localNow.AddYears(1),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            while (schedule.ScheduleExtension.OccurrenceCountLimit != 0 
                   && currentLookaheadCount < schedule.ScheduleExtension.LookaheadLimit
                   && schedule.ScheduleExtension.SchedulingAnchorUtc < maxLocalLookaheadTime)
            {
                var delta = schedule.ScheduleExtension.ScheduleFrequencyType switch
                {
                    ScheduleFrequencyType.Minutes => TimeSpan.FromMinutes(schedule.ScheduleExtension.FrequencyInterval),
                    ScheduleFrequencyType.Hours => TimeSpan.FromHours(schedule.ScheduleExtension.FrequencyInterval),
                    ScheduleFrequencyType.Days => TimeSpan.FromDays(schedule.ScheduleExtension.FrequencyInterval),
                    _ => throw new ArgumentOutOfRangeException()
                };

                do
                {
                    schedule.ScheduleExtension.NextDueAtUtc = schedule.ScheduleExtension.NextDueAtUtc is null 
                        ? schedule.ScheduleExtension.ActiveFromUtc
                        : schedule.ScheduleExtension.SchedulingAnchorUtc.Add(delta);

                    var localScheduledTimestamp = schedule.ScheduleExtension.SchedulingAnchorUtc;
                    var localScheduledTime = TimeOnly.FromTimeSpan(localScheduledTimestamp.TimeOfDay);
                    
                    if ((schedule.ScheduleExtension.StartTime is not null && localScheduledTime < schedule.ScheduleExtension.StartTime) 
                        || (schedule.ScheduleExtension.EndTime is not null && localScheduledTime > schedule.ScheduleExtension.EndTime))
                    {
                        continue;
                    }

                    if (schedule.ScheduleExtension.ByDay is not null &&
                        !schedule.ScheduleExtension.ByDay.Contains((int)localScheduledTimestamp.DayOfWeek))
                    {
                        continue;   
                    }

                    if (schedule.ScheduleExtension.ByMonthDay is not null && !schedule.ScheduleExtension.ByMonthDay.Contains(localScheduledTimestamp.Day))
                    {
                        continue;
                    }

                    if (schedule.ScheduleExtension.ByMonth is not null && !schedule.ScheduleExtension.ByMonth.Contains(localScheduledTimestamp.Month))
                    {
                        continue;
                    }
                    
                    break;
                } 
                while (schedule.ScheduleExtension.SchedulingAnchorUtc < maxLocalLookaheadTime);

                if (schedule.ScheduleExtension.SchedulingAnchorUtc > localNow)
                {
                    currentLookaheadCount++;
                }
                
                var log = schedule.ScheduleExtension.CreateExecutionLog();
                schedule.ScheduleExtension.ReduceOccurrenceCount();
                schedule.ScheduleExtension.CheckAndUpdateNextAllowedExecution();
                
                await repository.TryAddExecutionLogAsync(log, stoppingToken);
                
                await repository.SaveChangesAsync(stoppingToken);
            }
        }
    }
}