using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Descriptions;
using Misa.Contract.Main;
using Misa.Domain.Extensions;

namespace Misa.Application.Entities.Commands;

public class CreateDescriptionHandler(IMainRepository repository)
{
    public async Task CreateAsync(DescriptionResolvedDto resolvedDto)
    {
        var description = new Description(resolvedDto.EntityId, resolvedDto.Type.Id, resolvedDto.Content, DateTimeOffset.UtcNow);
        
        await repository.AddDescriptionAsync(description);
    }
}