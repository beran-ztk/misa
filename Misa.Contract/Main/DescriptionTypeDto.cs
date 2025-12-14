namespace Misa.Contract.Main;

public class DescriptionTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Synopsis { get; set; }
    public int SortOrder  { get; set; }
}