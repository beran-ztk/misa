using Misa.Contract.Items;
using Misa.Contract.Items.Components.Relations;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Relations;

namespace Misa.Application.Mappings;

public static class RelationMappings
{
    public static RelationType ToDomain(this RelationTypeDto dto) => dto switch
    {
        RelationTypeDto.RelatedTo   => RelationType.RelatedTo,
        RelationTypeDto.References  => RelationType.References,
        RelationTypeDto.DerivedFrom => RelationType.DerivedFrom,
        RelationTypeDto.DuplicateOf => RelationType.DuplicateOf,
        RelationTypeDto.Contains    => RelationType.Contains,
        _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
    };

    public static RelationTypeDto ToDto(this RelationType type) => type switch
    {
        RelationType.RelatedTo   => RelationTypeDto.RelatedTo,
        RelationType.References  => RelationTypeDto.References,
        RelationType.DerivedFrom => RelationTypeDto.DerivedFrom,
        RelationType.DuplicateOf => RelationTypeDto.DuplicateOf,
        RelationType.Contains    => RelationTypeDto.Contains,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static WorkflowDto ToWorkflowDto(this Workflow workflow) => workflow switch
    {
        Workflow.Task     => WorkflowDto.Task,
        Workflow.Schedule => WorkflowDto.Schedule,
        Workflow.Journal  => WorkflowDto.Journal,
        Workflow.Arc      => WorkflowDto.Arc,
        Workflow.Unit     => WorkflowDto.Unit,
        Workflow.Topic    => WorkflowDto.Topic,
        Workflow.Zettel   => WorkflowDto.Zettel,
        _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
    };
}
