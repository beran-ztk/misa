namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public record ListTaskDto
{
    public required Guid Id { get; init; }
    public string StateName { get; init; } = string.Empty;
    public string PriorityName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

