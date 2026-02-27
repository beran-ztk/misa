namespace Misa.Contract.Items.Components.Chronicle;

public sealed record ChronicleEntryDto(
    Guid? TargetItemId,
    DateTimeOffset At,
    string Title,
    string? Description,
    ChronicleEntryType Type
);