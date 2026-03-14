using Misa.Application.Abstractions.Persistence;

namespace Misa.Application.Features.Items.Zettelkasten;

public sealed record UpdateZettelContentCommand(Guid Id, string? Content);

public sealed class UpdateZettelContentHandler(IItemRepository repository)
{
    public async Task HandleAsync(UpdateZettelContentCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetZettelAsync(command.Id, ct);
        if (item?.ZettelExtension is null) return;

        item.ZettelExtension.ChangeContent(command.Content);
        await repository.SaveChangesAsync(ct);
    }
}
