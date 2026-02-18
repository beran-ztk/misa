using Misa.Application.Features.Chronicle;
using Misa.Contract.Features.Chronicle;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Audit;
using Wolverine;

namespace Misa.Api.Endpoints.Journals;

public static class GetJournalsEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("journals", GetJournals);
    }
    private static async Task<Result<List<JournalEntryDto>>> GetJournals(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<Result<List<JournalEntryDto>>>(new GetJournalEntriesQuery(), ct);
        return result;
    }
}