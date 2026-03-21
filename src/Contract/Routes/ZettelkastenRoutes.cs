namespace Misa.Contract.Routes;

public static class ZettelkastenRoutes
{
    public const string CreateTopic = "items/zettelkasten/topic";
    public const string GetZettelkasten = "items/zettelkasten";

    // Zettels
    public const string CreateZettel = "items/zettelkasten/zettel";
    public const string GetZettel = "items/zettelkasten/{itemId:guid}/zettel";
    public const string UpdateZettelContent = "items/zettelkasten/{itemId:guid}/zettel/content";

    public static string UpdateZettelContentUrl(Guid itemId) => $"items/zettelkasten/{itemId}/zettel/content";
}