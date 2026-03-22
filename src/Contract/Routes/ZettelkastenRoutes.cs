namespace Misa.Contract.Routes;

public static class ZettelkastenRoutes
{
    public const string CreateTopic = "items/zettelkasten/topic";
    public const string GetKnowledgeIndex        = "items/zettelkasten";
    public const string GetDeletedKnowledgeIndex = "items/zettelkasten/deleted";

    // Zettels
    public const string CreateZettel = "items/zettelkasten/zettel";
    public const string GetZettel = "items/zettelkasten/{itemId:guid}/zettel";
    public const string UpdateZettelContent = "items/zettelkasten/{itemId:guid}/zettel/content";

    public static string GetZettelUrl(Guid itemId) => $"items/zettelkasten/{itemId}/zettel";
    public static string UpdateZettelContentUrl(Guid itemId) => $"items/zettelkasten/{itemId}/zettel/content";

    public const string SetKnowledgeIndexExpanded = "items/zettelkasten/{itemId:guid}/expanded";
    public static string SetKnowledgeIndexExpandedUrl(Guid itemId) => $"items/zettelkasten/{itemId}/expanded";

    public const string ReparentKnowledgeItem = "items/zettelkasten/{itemId:guid}/parent";
    public static string ReparentKnowledgeItemUrl(Guid itemId) => $"items/zettelkasten/{itemId}/parent";
}