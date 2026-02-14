using Misa.Contract.Features.Common.Deadlines;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Domain.Features.Common;

namespace Misa.Application.Mappings;

public static class SharedMappings
{
    public static DeadlineDto ToDto(this Deadline entity) => new(entity.DueAt, entity.CreatedAt);
}