using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record UpdateZettelContentCommand(Guid Id, string? Content);

public sealed class UpdateZettelContentHandler(IItemRepository repository)
{
    public async Task HandleAsync(UpdateZettelContentCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetZettelAsync(command.Id, ct);
        if (item?.ZettelExtension is null) return;

        item.ChangeZettelContent(command.Content, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(ct);
    }
}
