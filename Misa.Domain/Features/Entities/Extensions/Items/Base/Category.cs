using Misa.Domain.Features.Entities.Base;

namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public class Category
{
    private Category() { }
    
    public int Id { get; set; }
    public Workflow Workflow { get; set; }
    public int WorkflowId { get; set; }
    public string Name { get; set; }
    public string? Synopsis { get; set; }
    public int SortOrder { get; set; }
}