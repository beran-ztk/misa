namespace Misa.Contract.Items.Components.Chronicle;

public sealed record ChronicleEntryDto(
    Guid? TargetItemId,
    DateTimeOffset At,
    string Title,
    ChronicleEntryType Type,
    ChronicleMetaState? MetaState,
    string? MetaText,
    string? Description
);