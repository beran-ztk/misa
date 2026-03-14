namespace Misa.Domain.Items.Components.Audits.Changes;

public enum ChangeType
{
    // Item
    Title,
    Description,

    // Activity
    State,
    Priority,
    Deadline,

    // TaskExtension
    Category,

    // ScheduleExtension
    MisfirePolicy,
    LookaheadLimit,
    OccurrenceCountLimit,
    StartTime,
    EndTime,
    ActiveUntil,

    // ZettelExtension
    Content,
}