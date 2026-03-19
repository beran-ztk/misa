using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;

namespace Misa.Application.Features.Items.Zettelkasten;

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
