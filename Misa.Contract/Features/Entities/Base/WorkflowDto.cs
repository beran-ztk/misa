namespace Misa.Contract.Features.Entities.Base;

public class WorkflowDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Synopsis { get; set; }
}