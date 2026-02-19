using System.Security.Claims;
using Misa.Application.Abstractions.Authentication;

namespace Misa.Api.Services.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    public bool IsAuthenticated =>
        _http.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? UserId =>
        _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}