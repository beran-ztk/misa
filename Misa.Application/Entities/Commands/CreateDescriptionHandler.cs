using Misa.Application.Main.Repositories;
using Misa.Contract.Main;
using Misa.Domain.Extensions;

namespace Misa.Application.Entities.Commands;

public class CreateDescriptionHandler(IMainRepository repository)
{
    public async Task CreateAsync(DescriptionDto dto)
    {
        var description = new Description(dto.EntityId, dto.TypeId, dto.Content, DateTimeOffset.UtcNow);
        
        await repository.AddDescriptionAsync(description);
    }
}