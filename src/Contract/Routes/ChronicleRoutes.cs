namespace Misa.Contract.Routes;

public static class ChronicleRoutes
{
    public const string CreateJournal = "items/journal";
    public const string GetChronicle  = "items/chronicle";

    public const string UpdateJournal = "items/{itemId:guid}/journal";
    public static string UpdateJournalRequest(Guid itemId) => $"items/{itemId}/journal";
}
