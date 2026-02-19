namespace Misa.Domain.Features.Audit;

public sealed class JournalCategory(JournalCategoryId id, string name, JournalId journalId, DateTimeOffset createdAt)
{
    public JournalCategoryId Id { get; } = id;
    public JournalId JournalId { get; } = journalId;
    public string Name { get; } = name;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}