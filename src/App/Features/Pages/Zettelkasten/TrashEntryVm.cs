using System;
using Misa.Contract.Items;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed class TrashEntryVm
{
    public Guid          Id           { get; init; }
    public WorkflowDto   Workflow     { get; init; }
    public string        Title        { get; init; } = string.Empty;
    public Guid?         ParentId     { get; init; }
    public string?       ParentTitle  { get; init; }
    public DateTimeOffset? DeletedAt  { get; init; }

    public string DeletedAtDisplay =>
        DeletedAt.HasValue ? DeletedAt.Value.LocalDateTime.ToString("MMM d, yyyy") : string.Empty;

    public string LocationDisplay =>
        ParentTitle is not null ? $"in {ParentTitle}" : "at root";
}
