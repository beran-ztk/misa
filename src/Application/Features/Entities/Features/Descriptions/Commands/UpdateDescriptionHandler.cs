using Misa.Application.Abstractions.Persistence;
using Misa.Application.Features.Entities.Extensions.Items.Base.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Features;

namespace Misa.Application.Features.Entities.Features.Descriptions.Commands;

public record UpdateDescriptionCommand(Guid DescriptionId, string Content);

public sealed class UpdateDescriptionHandler(IEntityRepository repository)
{
    public async Task<Result<DescriptionDto>> Handle(UpdateDescriptionCommand cmd, CancellationToken ct)
    {
        var description = await repository.GetDescriptionByIdAsync(cmd.DescriptionId, ct);
        if (description is null)
            return Result<DescriptionDto>.NotFound("Description not found","");

        description.UpdateContent(cmd.Content);

        await repository.SaveChangesAsync();

        return Result<DescriptionDto>.Ok(description.ToDto());
    }
}