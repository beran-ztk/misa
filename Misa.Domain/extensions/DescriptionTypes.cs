namespace Misa.Domain.Extensions;

public class DescriptionTypes
{
    private DescriptionTypes() { }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Synopsis { get; set; }
    public int SortOrder { get; set; }
}