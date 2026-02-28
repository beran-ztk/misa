namespace Misa.Domain.Items.Components.Chronicle.Journals;

public sealed class JournalExtension
{
    private JournalExtension() { } // EF

    public JournalExtension(DateTimeOffset occurredAt, DateTimeOffset? untilAt = null)
    {
        OccurredAt = occurredAt;
        UntilAt = untilAt;
    }

    public ItemId Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? UntilAt { get; private set; }
}