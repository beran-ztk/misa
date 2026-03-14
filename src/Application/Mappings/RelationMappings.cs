using Misa.Contract.Items.Components.Relations;
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
}
