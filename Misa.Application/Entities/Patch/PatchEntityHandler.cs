using Misa.Application.Entities.Repositories;

namespace Misa.Application.Entities.Patch;

public class PatchEntityHandler(IEntityRepository repository)
{
    public async Task DeleteEntityAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repository.GetTrackedEntityAsync(id);
        entity.Delete();
        await repository.SaveChangesAsync();
    }
    public async Task ArchiveEntityAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repository.GetTrackedEntityAsync(id);
        entity.Archive();
        await repository.SaveChangesAsync();
    }
}