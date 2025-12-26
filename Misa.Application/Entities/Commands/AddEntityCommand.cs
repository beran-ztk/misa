using Misa.Application.Entities.Mappings;
namespace Misa.Application.Entities.Add;

public static class AddEntityCommand
{
    public static Misa.Domain.Entities.Entity Transform(this Misa.Contract.Entities.CreateEntityDto createEntity)
        => createEntity.ToDomain();
}