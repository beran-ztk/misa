using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Chronicle;

public record GetJournalsCommand;

public sealed class GetJournalsHandler(IItemRepository repository)
{
    public async Task<List<ItemDto>> HandleAsync(GetJournalsCommand command, CancellationToken ct = default)
    {
        var journals = await repository.GetJournalsAsync();

        return journals.Select(j =>
        {
            var ext = j.JournalExtension
                      ?? throw new DomainConflictException("corrupted.data", "journal extension missing");

            var dto = j.ToDto();
            dto.JournalExtension = ext.ToDto();

            return dto;
        }).ToList();
    }
}