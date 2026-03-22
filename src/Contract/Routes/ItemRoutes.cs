namespace Misa.Contract.Routes;

public class ItemRoutes
{
    // Patch
    public const string ArchiveItem  = "items/{itemId:guid}/archive";
    public static string ArchiveItemRequest(Guid itemId)  => $"items/{itemId}/archive";

    public const string RestoreItem  = "items/{itemId:guid}/restore";
    public static string RestoreItemRequest(Guid itemId)  => $"items/{itemId}/restore";

    // Patch - rename
    public const string RenameItem = "items/{itemId:guid}/title";
    public static string RenameItemUrl(Guid itemId) => $"items/{itemId}/title";

    // Delete (soft)
    public const string DeleteItem = "items/{itemId:guid}";
    public static string DeleteItemRequest(Guid itemId) => $"items/{itemId}";

    // Delete (permanent / hard)
    public const string HardDeleteItem = "items/{itemId:guid}/permanent";
    public static string HardDeleteItemRequest(Guid itemId) => $"items/{itemId}/permanent";
}