namespace Misa.Contract.Items.Lookups;

public sealed class StateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Synopsis { get; set; }
}