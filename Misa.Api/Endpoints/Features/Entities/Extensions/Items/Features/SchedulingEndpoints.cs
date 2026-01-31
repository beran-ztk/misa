using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Wolverine;

namespace Misa.Api.Endpoints.Features.Entities.Extensions.Items.Features;

public static class SchedulingEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("scheduling", GetSchedulingRules);
        app.MapPost("scheduling", AddSchedulingRule);
    }

    private static async Task<Result<List<ScheduleDto>>> GetSchedulingRules(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<Result<List<ScheduleDto>>>(new GetScheduleQuery(), ct);
        return result;
    }
    private static async Task<Result<ScheduleDto>> AddSchedulingRule(
        [FromBody] AddScheduleDto dto, 
        IMessageBus bus, 
        CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            var command = new AddScheduleCommand(
                dto.Title,
                dto.ScheduleFrequencyType,
                dto.FrequencyInterval,
                dto.LookaheadLimit,
                dto.OccurrenceCountLimit,
                dto.MisfirePolicy,
                dto.OccurrenceTtl,
                dto.StartTime,
                dto.EndTime,
                dto.ActiveFromUtc,
                dto.ActiveUntilUtc,
                dto.ByDay,
                dto.ByMonthDay,
                dto.ByMonth
            );
            
            return await bus.InvokeAsync<Result<ScheduleDto>>(command, linkedCts.Token);
        }
        catch (Exception ex)
        {
            return Result<ScheduleDto>.Conflict("", ex.Message);
        }
    }
}