namespace Misa.Contract.Routes;

public static class RelationRoutes
{
    public const string CreateRelation       = "items/relations";
    public const string GetRelationsForItem  = "items/{itemId:guid}/relations";
    public const string GetItemsForLookup    = "items/lookup";
    public const string UpdateRelation       = "items/relations/{relationId:guid}";
    public const string DeleteRelation       = "items/relations/{relationId:guid}";

    public static string GetRelationsForItemUrl(Guid itemId)  => $"items/{itemId}/relations";
    public static string UpdateRelationUrl(Guid relationId)   => $"items/relations/{relationId}";
    public static string DeleteRelationUrl(Guid relationId)   => $"items/relations/{relationId}";
}
