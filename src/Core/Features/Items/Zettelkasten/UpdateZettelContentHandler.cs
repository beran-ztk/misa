using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record UpdateZettelContentCommand(Guid Id, string? Content);

public sealed class UpdateZettelContentHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(UpdateZettelContentCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetZettelAsync(command.Id, ct);
        if (item?.ZettelExtension is null) return;

        item.ChangeZettelContent(command.Content, timeProvider.UtcNow);
        await repository.SaveChangesAsync(ct);
    }
}
