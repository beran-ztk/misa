namespace Misa.Contract.Routes;

public class ItemRoutes
{
    // Patch
    public const string ArchiveItem = "items/{itemId:guid}/archive";
    public static string ArchiveItemRequest(Guid itemId) => $"items/{itemId}/archive";
    
    // Delete
    public const string DeleteItem = "items/{itemId:guid}";
    public static string DeleteItemRequest(Guid itemId) => $"items/{itemId}";
}