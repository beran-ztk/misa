namespace Resona.Cloud.Server;

public static class PublicLibraryEndpoints
{
    public static IEndpointRouteBuilder MapPublicLibraryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/public");

        group.MapGet("/profiles", async (
            string? search,
            int? offset,
            int? limit,
            CloudPublicLibraryRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (!PublicLibraryQuery.TryCreate(search, offset, limit, out var query, out var error))
                return Results.BadRequest(new { error });

            return Results.Ok(await repository.GetProfilesAsync(query, cancellationToken));
        });

        group.MapGet("/profiles/{userId:guid}", async (
            Guid userId,
            CloudPublicLibraryRepository repository,
            CancellationToken cancellationToken) =>
        {
            var profile = await repository.GetProfileAsync(userId, cancellationToken);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        });

        group.MapGet("/profiles/{userId:guid}/image", async (
            Guid userId,
            CloudPublicLibraryRepository repository,
            CancellationToken cancellationToken) =>
        {
            var image = await repository.GetProfileImageAsync(userId, cancellationToken);
            return image is null
                ? Results.NotFound()
                : Results.File(image, "image/jpeg");
        });

        group.MapGet("/profiles/{userId:guid}/tracks", async (
            Guid userId,
            string? search,
            int? offset,
            int? limit,
            CloudPublicLibraryRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (!PublicLibraryQuery.TryCreate(search, offset, limit, out var query, out var error))
                return Results.BadRequest(new { error });

            var tracks = await repository.GetTracksAsync(userId, query, cancellationToken);
            return tracks is null ? Results.NotFound() : Results.Ok(tracks);
        });

        return endpoints;
    }
}
