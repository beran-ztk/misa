namespace Misa.Contract.Audit.Lookups;

public class SessionStateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Synopsis { get; set; }
}