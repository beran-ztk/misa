namespace Misa.Contract.Features.Actions;

public class ActionDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }

    

    public string? ValueBefore { get; set; }
    public string? ValueAfter { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string GetValueBefore => ValueBefore ?? "No Value";
    public string GetValueAfter => ValueAfter ?? "No Value";
}