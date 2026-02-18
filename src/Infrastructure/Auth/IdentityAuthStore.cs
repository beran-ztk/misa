using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Misa.Application.Abstractions.Authentication;
using Misa.Contract.Shared.Results;

namespace Misa.Infrastructure.Auth;

public sealed class IdentityAuthStore(UserManager<User> userManager) : IIdentityAuthStore
{
    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct)
    {
        var existing = await userManager.FindByNameAsync(username);
        return existing is not null;
    }

    public async Task<Result<IdentityUserCreated>> CreateUserAsync(
        Guid id,
        string email,
        string username,
        string password,
        string timeZone,
        CancellationToken ct)
    {
        var user = new User
        {
            Id = id.ToString("D"),
            UserName = username,
            Email = email
        };

        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
            return Result<IdentityUserCreated>.Conflict("", JoinErrors(create.Errors));

        // TimeZone ohne Custom-Feld: als Claim speichern
        var claimRes = await userManager.AddClaimAsync(user, new Claim("tz", timeZone));
        
        return !claimRes.Succeeded 
            ? Result<IdentityUserCreated>.Conflict("", JoinErrors(claimRes.Errors)) 
            : Result<IdentityUserCreated>.Ok(new IdentityUserCreated(id, username, email));
    }

    private static string JoinErrors(IEnumerable<IdentityError> errors)
        => string.Join("; ", errors.Select(e => e.Description));
}