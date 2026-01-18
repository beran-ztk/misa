using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IScheduleRepository
{
    Task<Scheduler> AddSchedulingRule(CancellationToken ct);
}