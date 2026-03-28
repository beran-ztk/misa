using Misa.Contract.Items;
using Misa.Contract.Items.Components.Audits;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Audits.Changes;
using Misa.Domain.Items.Components.Chronicle.Journals;

namespace Misa.Core.Mappings;
public static class ItemMappings
{
    public static ItemDto ToDto(this Item item)
    {
        return new ItemDto
        {
            Id = item.Id.Value,
            Workflow = item.Workflow.ToDto(),

            Title = item.Title,
            Description = item.Description,

            IsDeleted = item.IsDeleted,
            IsArchived = item.IsArchived,

            CreatedAt = item.CreatedAt,
            ModifiedAt = item.ModifiedAt,

            Changes = item.Changes
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => c.ToDto())
                .ToList()
        };
    }

    public static AuditChangeDto ToDto(this AuditChange change) =>
        new(
            change.Id,
            change.ItemId.Value,
            change.ChangeType.ToString(),
            change.ValueBefore,
            change.ValueAfter,
            change.Reason,
            change.CreatedAtUtc
        );
    public static JournalExtensionDto ToDto(this JournalExtension journal)
    {
        return new JournalExtensionDto(
            journal.Id.Value,
            journal.OccurredAt,
            journal.UntilAt
        );
    }
    public static ItemDto TaskToItemDto(this Item item)
    {
        if (item.Workflow != Workflow.Task || item.Activity == null || item.TaskExtension == null)
            throw new DomainValidationException(nameof(item.TaskExtension), "", "Could not map task to dto, because of missing data.");
        
        var temp = item.ToDto();
        temp.Activity = item.Activity.ToDto();
        temp.TaskExtension = item.TaskExtension.ToDto();
        return temp;
    }
    public static ItemDto ScheduleToItemDto(this Item item)
    {
        if (item.Workflow != Workflow.Schedule || item.ScheduleExtension == null)
            throw new DomainValidationException(nameof(item.TaskExtension), "", "Could not map task to dto, because of missing data.");
        
        var temp = item.ToDto();
        temp.ScheduleExtension = item.ScheduleExtension.ToDto();
        return temp;
    }
    private static WorkflowDto ToDto(this Workflow workflow) =>
        workflow switch
        {
            Workflow.Task => WorkflowDto.Task,
            Workflow.Schedule => WorkflowDto.Schedule,
            Workflow.Journal  => WorkflowDto.Journal,
            Workflow.Arc  => WorkflowDto.Arc,
            Workflow.Unit  => WorkflowDto.Unit,
            Workflow.Zettel  => WorkflowDto.Zettel,
            _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
        };
}