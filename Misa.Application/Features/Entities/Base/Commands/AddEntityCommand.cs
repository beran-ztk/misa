using Misa.Application.Features.Entities.Base.Mappings;
using Misa.Contract.Features.Entities.Base;
using Misa.Domain.Features.Entities.Base;

namespace Misa.Application.Features.Entities.Base.Commands;

public static class AddEntityCommand
{
    public static Entity Transform(this CreateEntityDto createEntity)
        => createEntity.ToDomain();
}