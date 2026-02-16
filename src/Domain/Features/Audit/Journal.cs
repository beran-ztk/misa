using Misa.Domain.Features.Users;

namespace Misa.Domain.Features.Audit;

// System-Types
public enum JournalSystemType
{
    Session
}

// Strong-Types
public readonly record struct JournalId(Guid Value);
public readonly record struct JournalEntryId(Guid Value);
public readonly record struct JournalCategoryId(Guid Value);

// Journal-Category
public sealed class JournalCategory(JournalCategoryId id, string name, JournalId journalId, DateTimeOffset createdAt)
{
    public JournalCategoryId Id { get; } = id;
    public JournalId JournalId { get; } = journalId;
    public string Name { get; } = name;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}

// Journal-Entry
public sealed class JournalEntry(
    JournalEntryId id,
    JournalId journalId,
    string description,
    DateTimeOffset occurredAt,
    DateTimeOffset? untilAt,
    DateTimeOffset createdAt,
    Guid? originId,
    JournalSystemType? systemType,
    JournalCategoryId? categoryId)
{
    public JournalEntryId Id { get; } = id;

    public JournalId JournalId { get; } = journalId;
    // Timeline
    public DateTimeOffset OccurredAt { get; } = occurredAt;
    public DateTimeOffset? UntilAt { get; } = untilAt;

    // Audit
    public DateTimeOffset CreatedAt { get; } = createdAt;

    // Content
    public string Description { get; } = description;

    // System-only
    public Guid? OriginId { get; } = originId;
    public JournalSystemType? SystemType { get; } = systemType;

    // User-only
    public JournalCategoryId? CategoryId { get; } = categoryId;
}

public sealed class Journal(JournalId id, Guid userId, DateTimeOffset createdAt)
{
    public List<JournalEntry> Entries = [];
    public List<JournalCategory> Categories = [];
    public User User { get; init; }
    
    public JournalId Id { get; } = id;
    public Guid UserId { get; } = userId;
    public DateTimeOffset CreatedAt { get; } = createdAt;

    public JournalEntry AddEntry(
        JournalEntryId entryId,
        string description,
        DateTimeOffset occurredAt,
        DateTimeOffset? untilAt,
        DateTimeOffset createdAt,
        Guid? originId,
        JournalSystemType? systemType,
        JournalCategoryId? categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Description is required.");

        if (untilAt is not null && untilAt.Value < occurredAt)
            throw new InvalidOperationException("UntilAt must be >= OccurredAt.");

        if (categoryId is not null && Categories.All(c => c.Id != categoryId.Value))
            throw new InvalidOperationException("Category does not exist.");

        var entry = new JournalEntry(
            id: entryId,
            journalId: Id,
            description: description.Trim(),
            occurredAt: occurredAt,
            untilAt: untilAt,
            createdAt: createdAt,
            originId: originId,
            systemType: systemType,
            categoryId: categoryId
        );

        Entries.Add(entry);
        return entry;
    }
    public JournalCategory CreateCategory(JournalCategoryId categoryId, string name, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Category name is required.");

        name = name.Trim();

        if (Categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Category name must be unique per journal.");

        var category = new JournalCategory(categoryId, name, Id, createdAt);
        Categories.Add(category);
        return category;
    }
}
