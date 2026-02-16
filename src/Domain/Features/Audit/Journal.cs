using System.Collections.ObjectModel;

namespace Misa.Domain.Features.Audit;

public readonly record struct JournalId(Guid Value);
public readonly record struct JournalEntryId(Guid Value);
public readonly record struct JournalCategoryId(Guid Value);

