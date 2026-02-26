namespace Misa.Contract.Items.Components.Tasks;

public sealed record TaskExtensionDto
{
    public required Guid Id { get; init; }
    public required TaskCategoryDto Category { get; init; }
}