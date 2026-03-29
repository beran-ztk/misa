using Misa.Core.Persistence;

namespace Misa.Core.Features.Dev;

public record DeleteUserDataCommand;

public sealed class DeleteUserDataHandler(ItemRepository repository)
{
    public async Task HandleAsync(DeleteUserDataCommand command, CancellationToken ct)
    {
        await repository.DeleteAllByOwnerAsync(ct);
    }
}
