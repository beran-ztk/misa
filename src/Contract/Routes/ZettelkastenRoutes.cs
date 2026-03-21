namespace Misa.Contract.Routes;

public static class ZettelkastenRoutes
{
    // Topics
    public const string CreateTopic = "items/zettelkasten/topic";
    public const string GetKnowledgeIndex = "zettelkasten/index";

    // Zettels
    public const string CreateZettel = "items/zettelkasten/zettel";
    public const string GetZettels = "items/zettelkasten/zettels";
    public const string GetZettel = "items/zettelkasten/{itemId:guid}/zettel";
    public const string UpdateZettelContent = "items/zettelkasten/{itemId:guid}/zettel/content";

    public static string UpdateZettelContentUrl(Guid itemId) => $"items/zettelkasten/{itemId}/zettel/content";
}