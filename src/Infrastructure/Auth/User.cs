using Microsoft.AspNetCore.Identity;

namespace Misa.Infrastructure.Auth;

public sealed class User : IdentityUser
{
    public required string TimeZone { get; init; }
};