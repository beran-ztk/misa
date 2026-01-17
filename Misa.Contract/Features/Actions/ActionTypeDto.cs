namespace Misa.Contract.Features.Actions;

public class ActionTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Synopsis { get; set; }
}