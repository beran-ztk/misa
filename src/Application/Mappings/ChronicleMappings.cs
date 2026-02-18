using Misa.Contract.Features.Chronicle;
using Misa.Domain.Features.Audit;

namespace Misa.Application.Mappings;

public static class ChronicleMappings
{
    public static List<JournalEntryDto> ToDto(this List<JournalEntry> journals)
    {
        return journals.Select(t => t.ToDto()).ToList();
    }
    public static JournalEntryDto ToDto(this JournalEntry entry)
    {
        return new JournalEntryDto(
            Id: entry.Id.Value,
            JournalId: entry.JournalId.Value,

            Description: entry.Description,

            OccurredAt: entry.OccurredAt,
            UntilAt: entry.UntilAt,

            CreatedAt: entry.CreatedAt,

            OriginId: entry.OriginId,

            SystemType: null,

            CategoryId: entry.CategoryId?.Value
        );
    }
}