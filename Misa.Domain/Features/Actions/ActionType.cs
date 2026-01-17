namespace Misa.Domain.Features.Actions;

public class ActionType
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Synopsis { get; private set; }
}