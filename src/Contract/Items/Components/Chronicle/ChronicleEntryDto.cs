namespace Misa.Contract.Items.Components.Chronicle;

public sealed record ChronicleEntryDto(
    Guid? TargetItemId,
    DateTime At,
    string Title,
    string? Description,
    ChronicleEntryType Type
);