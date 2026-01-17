namespace Misa.Application.Features.Entities.Features.Descriptions.Commands;

public record AddDescriptionCommand(Guid EntityId, string Content);