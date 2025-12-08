namespace Misa.Domain.Items;

public class State
{
    private State() { }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Synopsis { get; set; }
    public int SortOrder { get; set; }
}