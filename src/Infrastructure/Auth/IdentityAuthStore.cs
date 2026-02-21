using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Misa.Application.Abstractions.Authentication;
using Misa.Contract.Shared.Results;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Misa.Infrastructure.Auth;

public sealed class IdentityAuthStore(UserManager<User> userManager,
    IConfiguration configuration) : IIdentityAuthStore
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
            Email = email,
            TimeZone = timeZone
        };

        var create = await userManager.CreateAsync(user, password);
        return !create.Succeeded 
            ? Result<IdentityUserCreated>.Conflict("", JoinErrors(create.Errors)) 
            : Result<IdentityUserCreated>.Ok(new IdentityUserCreated(id, username, email));
    }
    public async Task<Result<IdentityLoginResult>> LoginAsync(
        string username,
        string password,
        CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
            return Result<IdentityLoginResult>.Conflict("", "Invalid credentials.");

        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return Result<IdentityLoginResult>.Conflict("", "Invalid credentials.");

        var token = GenerateToken(user);

        return Result<IdentityLoginResult>.Ok(
            new IdentityLoginResult(
                Guid.Parse(user.Id),
                user.UserName!,
                token,
                user.TimeZone));
    }
    private string GenerateToken(User user)
    {
        var jwtSection = configuration.GetSection("Jwt");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSection["Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new("tz", user.TimeZone)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string JoinErrors(IEnumerable<IdentityError> errors)
        => string.Join("; ", errors.Select(e => e.Description));
}