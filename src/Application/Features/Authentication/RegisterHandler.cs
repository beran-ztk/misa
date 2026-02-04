using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Authentication;
using Misa.Domain.Features.Users;

namespace Misa.Application.Features.Authentication;
public sealed record RegisterCommand(string Username, string Password, string TimeZone);

public sealed class RegisterHandler(IAuthenticationRepository repository)
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var username = cmd.Username.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cmd.Password))
            return Result<AuthResponseDto>.Conflict("","Username und Password dürfen nicht leer sein.");

        var existing = await repository.FindByUsernameAsync(username, ct);
        if (existing is not null)
            return Result<AuthResponseDto>.Conflict("","Username ist bereits vergeben.");

        var tz = string.IsNullOrWhiteSpace(cmd.TimeZone) ? "Europe/Berlin" : cmd.TimeZone.Trim();

        var user = new User(username, cmd.Password, tz);
        await repository.AddAsync(user, ct);

        var dto = new UserDto(user.Id, user.Username, user.TimeZone);
        return Result<AuthResponseDto>.Ok(new AuthResponseDto(dto));
    }
}