using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Features.Authentication;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Users;

namespace Misa.Application.Features.Authentication;
public sealed record RegisterCommand(string Username, string Password, string TimeZone);

public sealed class RegisterHandler(IAuthenticationRepository repository, ITimeZoneProvider timeZoneProvider)
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var username = cmd.Username.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cmd.Password))
            return Result<AuthResponseDto>.Conflict("","Username und Password dürfen nicht leer sein.");

        var existing = await repository.FindByUsernameAsync(username, ct);
        if (existing is not null)
            return Result<AuthResponseDto>.Conflict("","Username ist bereits vergeben.");

        if (!timeZoneProvider.IsValid(cmd.TimeZone))
            return Result<AuthResponseDto>.Conflict("","Ungültige Zeitzone.");
        
        var user = new User(username, cmd.Password, cmd.TimeZone);
        await repository.AddAsync(user, ct);

        var dto = new UserDto(user.Id, user.Username, user.TimeZone);
        return Result<AuthResponseDto>.Ok(new AuthResponseDto(dto));
    }
}