namespace Misa.Contract.Items.Components.Schola;

public record ScholaDto
{
    public List<ItemDto> Arcs { get; set; } = [];
    public List<ItemDto> Units { get; set; } = [];
};