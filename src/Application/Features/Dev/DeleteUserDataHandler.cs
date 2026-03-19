using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;

namespace Misa.Application.Features.Dev;

public record DeleteUserDataCommand;

public sealed class DeleteUserDataHandler(
    IItemRepository repository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(DeleteUserDataCommand command, CancellationToken ct)
    {
        await repository.DeleteAllByOwnerAsync(currentUser.Id, ct);
    }
}
