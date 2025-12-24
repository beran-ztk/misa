namespace Misa.Contract.Audit;

public class StopSessionDto
{
    public Guid EntityId { get; set; }
    public int? Efficiency { get; set; }
    public int? Concentration { get; set; }
    public string? Summary { get; set; }
}