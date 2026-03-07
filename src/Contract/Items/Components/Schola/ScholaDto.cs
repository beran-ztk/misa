namespace Misa.Contract.Items.Components.Schola;

public record ScholaDto
{
    public List<ArcDto> Arcs { get; set; } = [];
    public List<UnitDto> Units { get; set; } = [];
};