namespace Misa.Contract.Items.Components.Chronicle;

public record UpdateJournalRequest(
    string?        Description,
    DateTimeOffset OccurredAtUtc);
