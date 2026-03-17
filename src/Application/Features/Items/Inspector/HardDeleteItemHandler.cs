using Misa.Application.Abstractions.Persistence;

namespace Misa.Application.Features.Items.Inspector;

public record HardDeleteItemCommand(Guid Id);

public sealed class HardDeleteItemHandler(IItemRepository repository)
{
    public async Task HandleAsync(HardDeleteItemCommand command)
    {
        await repository.HardDeleteItemAsync(command.Id, CancellationToken.None);
    }
}
