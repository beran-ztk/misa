using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Chronicle;
using Misa.Contract.Features.Chronicle;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Audit;
using Wolverine;

namespace Misa.Api.Endpoints.Journals;

public static class CreateJournalEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("journals/{userId:guid}", AddJournalEntry);
    }

    private static async Task<Result<JournalEntryDto>> AddJournalEntry(
        [FromRoute] Guid userId,
        [FromBody] CreateJournalEntryDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new CreateJournalEntryCommand(
            userId,
            dto.Description,
            dto.OccurredAt,
            dto.UntilAt,
            dto.CategoryId);

        var result = await bus.InvokeAsync<Result<JournalEntryDto>>(command, ct);
        return result;
    }
}
