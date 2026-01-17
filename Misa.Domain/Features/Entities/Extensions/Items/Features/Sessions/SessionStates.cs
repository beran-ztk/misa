namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

public class SessionStates
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Synopsis { get; private set; }
}