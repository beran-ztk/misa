using Misa.Application.Abstractions.Persistence;

namespace Misa.Application.Features.Dev;

public record DeleteUserDataCommand;

public sealed class DeleteUserDataHandler(IItemRepository repository)
{
    public async Task HandleAsync(DeleteUserDataCommand command, CancellationToken ct)
    {
        await repository.DeleteAllByOwnerAsync(ct);
    }
}
