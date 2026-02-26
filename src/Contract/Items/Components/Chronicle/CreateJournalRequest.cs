namespace Misa.Contract.Items.Components.Chronicle;

public record CreateJournalRequest(    
    string Title,
    string? Description,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? UntilAtUtc);