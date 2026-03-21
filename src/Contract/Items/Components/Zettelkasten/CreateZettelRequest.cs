namespace Misa.Contract.Items.Components.Zettelkasten;

public sealed record CreateZettelRequest(string Title, Guid ParentId);
