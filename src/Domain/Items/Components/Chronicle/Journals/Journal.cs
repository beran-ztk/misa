namespace Misa.Domain.Features.Audit;

// System-Types
public enum JournalSystemType
{
    Session
}

// public sealed class JournalEntry(
//     JournalEntryId id,
//     JournalId journalId,
//     string description,
//     DateTimeOffset occurredAt,
//     DateTimeOffset? untilAt,
//     DateTimeOffset createdAt,
//     Guid? originId,
//     JournalSystemType? systemType,
//     JournalCategoryId? categoryId)
// {
//     public JournalEntryId Id { get; } = id;
//
//     public JournalId JournalId { get; } = journalId;
//     // Timeline
//     public DateTimeOffset OccurredAt { get; } = occurredAt;
//     public DateTimeOffset? UntilAt { get; } = untilAt;
//
//     // Audit
//     public DateTimeOffset CreatedAt { get; } = createdAt;
//
//     // Content
//     public string Description { get; } = description;
//
//     // System-only
//     public Guid? OriginId { get; } = originId;
//     public JournalSystemType? SystemType { get; } = systemType;
//
//     // User-only
//     public JournalCategoryId? CategoryId { get; } = categoryId;
// }
