namespace Misa.Contract.Items.Components.Zettelkasten;

public sealed record CreateTopicRequest(string Title, Guid? ParentId);