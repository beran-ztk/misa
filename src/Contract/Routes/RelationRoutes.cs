namespace Misa.Contract.Routes;

public static class RelationRoutes
{
    public const string CreateRelation = "items/relations";
    public const string GetRelationsForItem = "items/{itemId:guid}/relations";
    public const string GetItemsForLookup = "items/lookup";

    public static string GetRelationsForItemUrl(Guid itemId) => $"items/{itemId}/relations";
}
