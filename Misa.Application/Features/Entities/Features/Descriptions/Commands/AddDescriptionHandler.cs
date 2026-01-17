using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Extensions.Items.Base.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Features;
using Misa.Domain.Features.Entities.Features.Descriptions;

namespace Misa.Application.Features.Entities.Features.Descriptions.Commands;

public class AddDescriptionHandler(IEntityRepository repository)
{
    public async Task<Result<DescriptionDto>> Handle(AddDescriptionCommand cmd, CancellationToken ct)
    {
        var description = Description.Create(cmd.EntityId, cmd.Content);
        
        await repository.AddDescriptionAsync(description, ct);
        await repository.SaveChangesAsync();

        var descriptionDto = description.ToDto();

        return Result<DescriptionDto>.Ok(descriptionDto);
    }
}