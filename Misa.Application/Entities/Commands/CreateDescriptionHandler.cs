using Misa.Application.Entities.Mappings;
using Misa.Application.Main.Mappings;
using Misa.Application.Main.Repositories;
using Misa.Contract.Entities;
using Misa.Contract.Main;
using Misa.Domain.Main;

namespace Misa.Application.Main.Add;

public class CreateDescriptionHandler(IMainRepository repository)
{
    public async Task CreateAsync(DescriptionDto dto)
    {
        var description = new Description(dto.EntityId, dto.TypeId, dto.Content, DateTimeOffset.UtcNow);
        
        await repository.AddDescriptionAsync(description);
    }
}