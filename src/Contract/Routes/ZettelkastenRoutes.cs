namespace Misa.Contract.Routes;

public static class ZettelkastenRoutes
{
    // Topics
    public const string CreateTopic = "items/topic";
    public const string GetTopics = "items/topics";

    // Zettels
    public const string CreateZettel = "items/zettel";
    public const string GetZettels = "items/zettels";
    public const string GetZettel = "items/{itemId:guid}/zettel";
}