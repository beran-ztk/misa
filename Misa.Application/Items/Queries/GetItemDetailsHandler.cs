using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Items.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Details;

namespace Misa.Application.Items.Queries;

public class GetItemDetailsHandler(IItemRepository repository)
{
    public async Task<Result<ItemOverviewDto>> Handle(GetItemDetailsQuery cmd, CancellationToken ct)
    {
        var details = await repository.TryGetItemDetailsAsync(cmd.Id, ct);

        if (details == null)
            return Result<ItemOverviewDto>.NotFound("Item-Details", "Details not found.");
        
        var result = details.ToOverviewDto();

        return Result<ItemOverviewDto>
            .Ok(result);
    }
}