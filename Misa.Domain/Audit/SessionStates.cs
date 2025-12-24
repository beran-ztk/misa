namespace Misa.Domain.Audit;

public class SessionStates
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Synopsis { get; private set; }
}