namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

public sealed record CreateOnceScheduleDto(
    Guid TargetItemId,
    DateTimeOffset DueAtUtc
);