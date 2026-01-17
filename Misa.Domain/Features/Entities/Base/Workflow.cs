namespace Misa.Domain.Features.Entities.Base;

public class Workflow
{
    private Workflow() { }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Synopsis { get; set; }
}