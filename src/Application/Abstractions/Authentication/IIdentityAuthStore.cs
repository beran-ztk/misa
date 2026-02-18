using Misa.Contract.Shared.Results;

namespace Misa.Application.Abstractions.Authentication;

public interface IIdentityAuthStore
{
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct);

    Task<Result<IdentityUserCreated>> CreateUserAsync(
        Guid id,
        string email,
        string username,
        string password,
        string timeZone,
        CancellationToken ct);
}

public sealed record IdentityUserCreated(Guid UserId, string Username, string Email);