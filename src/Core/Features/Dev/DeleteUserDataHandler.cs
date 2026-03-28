using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Dev;

public record DeleteUserDataCommand;

public sealed class DeleteUserDataHandler(IItemRepository repository)
{
    public async Task HandleAsync(DeleteUserDataCommand command, CancellationToken ct)
    {
        await repository.DeleteAllByOwnerAsync(ct);
    }
}
