using System.Security.Claims;
using Misa.Application.Abstractions.Authentication;

namespace Misa.Api.Services.Auth;

public sealed class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    public string UserId =>
        http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated request without NameIdentifier claim.");
}