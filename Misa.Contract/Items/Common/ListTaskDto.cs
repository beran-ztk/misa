namespace Misa.Contract.Items.Common;

public record ListTaskDto
{
    public required Guid EntityId { get; init; }
    public string StateName { get; init; } = string.Empty;
    public string PriorityName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

