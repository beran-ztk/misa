namespace Misa.Contract.Items.Components.Zettelkasten;

public record ZettelDto(Guid Id, string Title, string? Content, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt);
