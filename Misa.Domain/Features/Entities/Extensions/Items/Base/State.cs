namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public class State
{
    private State() { }
    public State(int id, string name, string? synopsis)
    {
        Id = id;
        Name = name;
        Synopsis = synopsis;
    }
    public int Id { get; init; }
    public string Name { get; init; }
    public string? Synopsis { get; init; }
}