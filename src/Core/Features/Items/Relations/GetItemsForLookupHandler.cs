using Misa.Contract.Items;
using Misa.Contract.Items.Components.Relations;
using Misa.Core.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Relations;

public sealed record GetItemsForLookupQuery;

public sealed class GetItemsForLookupHandler(IItemRepository repository)
{
    public async Task<List<ItemLookupDto>> HandleAsync(GetItemsForLookupQuery query, CancellationToken ct)
    {
        var items = await repository.GetItemsForLookupAsync(ct);

        return items.Select(i => new ItemLookupDto(
            i.Id.Value,
            i.Title,
            i.Workflow switch
            {
                Workflow.Task     => WorkflowDto.Task,
                Workflow.Schedule => WorkflowDto.Schedule,
                Workflow.Journal  => WorkflowDto.Journal,
                Workflow.Arc      => WorkflowDto.Arc,
                Workflow.Unit     => WorkflowDto.Unit,
                Workflow.Topic    => WorkflowDto.Topic,
                Workflow.Zettel   => WorkflowDto.Zettel,
                _                 => WorkflowDto.Task
            }
        )).ToList();
    }
}
