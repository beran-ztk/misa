using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Common.Mappings;

public static class ScheduleMappings
{
    public static ScheduleFrequencyType MapToDomain(this ScheduleFrequencyTypeContract contract) =>
        contract switch
        {
            ScheduleFrequencyTypeContract.Once    => ScheduleFrequencyType.Once,
            ScheduleFrequencyTypeContract.Minutes => ScheduleFrequencyType.Minutes,
            ScheduleFrequencyTypeContract.Hours   => ScheduleFrequencyType.Hours,
            ScheduleFrequencyTypeContract.Days    => ScheduleFrequencyType.Days,
            ScheduleFrequencyTypeContract.Weeks   => ScheduleFrequencyType.Weeks,
            ScheduleFrequencyTypeContract.Months  => ScheduleFrequencyType.Months,
            ScheduleFrequencyTypeContract.Years   => ScheduleFrequencyType.Years,
            _ => throw new ArgumentOutOfRangeException(nameof(contract), contract, null)
        };
    public static ScheduleFrequencyTypeContract MapToDto(this ScheduleFrequencyType domain) =>
        domain switch
        {
            ScheduleFrequencyType.Once    => ScheduleFrequencyTypeContract.Once,
            ScheduleFrequencyType.Minutes => ScheduleFrequencyTypeContract.Minutes,
            ScheduleFrequencyType.Hours   => ScheduleFrequencyTypeContract.Hours,
            ScheduleFrequencyType.Days    => ScheduleFrequencyTypeContract.Days,
            ScheduleFrequencyType.Weeks   => ScheduleFrequencyTypeContract.Weeks,
            ScheduleFrequencyType.Months  => ScheduleFrequencyTypeContract.Months,
            ScheduleFrequencyType.Years   => ScheduleFrequencyTypeContract.Years,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
}