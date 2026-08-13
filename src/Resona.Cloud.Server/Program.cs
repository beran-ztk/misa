using Microsoft.AspNetCore.Http.Features;
using Npgsql;
using Resona.Cloud.Server;
using Resona.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 25 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 25 * 1024 * 1024);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddSingleton<CloudSnapshotRepository>();
builder.Services.AddSingleton<CloudPublicLibraryRepository>();

var app = builder.Build();
await CloudSchema.InitializeAsync(app.Services.GetRequiredService<NpgsqlDataSource>());

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPublicLibraryEndpoints();

app.MapPut("/api/v1/library-snapshot", async (
    HttpRequest request,
    CloudLibrarySnapshot snapshot,
    CloudSnapshotRepository repository,
    CancellationToken cancellationToken) =>
{
    var validationErrors = CloudSnapshotValidator.Validate(snapshot);
    if (validationErrors.Count > 0)
        return Results.ValidationProblem(validationErrors);

    if (!DeviceCredentialsReader.TryRead(request.Headers, out var credentials, out _))
        return Results.Unauthorized();
    if (!string.Equals(credentials.UserId.ToString("D"), snapshot.Profile.UserId, StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Authenticated user ID does not match snapshot profile." });

    var result = await repository.ReplaceSnapshotAsync(credentials, snapshot, cancellationToken);
    return result switch
    {
        SnapshotReplaceResult.Unauthorized => Results.Unauthorized(),
        SnapshotReplaceResult.DeviceConflict => Results.Conflict(new
        {
            error = "This device is not registered for the existing user. Device recovery is not implemented yet."
        }),
        SnapshotReplaceResult.EmptySnapshotRejected => Results.Conflict(new
        {
            error = "An empty snapshot cannot replace a previously populated library without explicit deletion support."
        }),
        _ => Results.Ok(new
        {
            userId = snapshot.Profile.UserId,
            trackCount = snapshot.TrackCount,
            synchronizedAt = DateTime.UtcNow
        })
    };
});

app.Run();

public partial class Program;
