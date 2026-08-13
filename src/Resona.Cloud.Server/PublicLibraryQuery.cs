namespace Resona.Cloud.Server;

public sealed record PublicLibraryQuery(string Search, int Offset, int Limit)
{
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 100;
    public const int MaximumSearchLength = 100;

    public static bool TryCreate(
        string? search,
        int? offset,
        int? limit,
        out PublicLibraryQuery query,
        out string? error)
    {
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedOffset = offset ?? 0;
        var normalizedLimit = limit ?? DefaultLimit;

        if (normalizedSearch.Length > MaximumSearchLength)
        {
            query = null!;
            error = $"Search text must not exceed {MaximumSearchLength} characters.";
            return false;
        }

        if (normalizedOffset < 0)
        {
            query = null!;
            error = "Offset must be zero or greater.";
            return false;
        }

        if (normalizedLimit is < 1 or > MaximumLimit)
        {
            query = null!;
            error = $"Limit must be between 1 and {MaximumLimit}.";
            return false;
        }

        query = new PublicLibraryQuery(normalizedSearch, normalizedOffset, normalizedLimit);
        error = null;
        return true;
    }
}
