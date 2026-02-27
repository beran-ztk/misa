using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Chronicle;

public record GetJournalsCommand;

public sealed class GetJournalsHandler(IItemRepository repository)
{
    public async Task<List<ItemDto>> HandleAsync(GetJournalsCommand command)
    {
        var journals = await repository.GetJournalsAsync();

        var result = new List<ItemDto>();

        foreach (var j in journals)
        {
            var ext = j.JournalExtension
                      ?? throw new DomainConflictException("corrupted.data", "journal extension missing");

            var dto = j.ToDto();
            dto.JournalExtension = ext.ToDto();

            result.Add(dto);
        }

        return result;
    }
}