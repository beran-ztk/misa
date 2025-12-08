using Misa.Domain.Dictionaries.Entities;
using Misa.Domain.Entities;

namespace Misa.Domain.Items;

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