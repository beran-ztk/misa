namespace Misa.Domain.Entities;

public class Workflow
{
    private Workflow() { }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Synopsis { get; set; }
}