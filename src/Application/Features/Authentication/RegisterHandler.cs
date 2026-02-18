using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Features.Authentication;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Audit;

namespace Misa.Application.Features.Authentication;

public sealed record RegisterCommand(string Email, string Username, string Password, string TimeZone);

public sealed class RegisterHandler(
    IIdentityAuthStore authStore,
    IChronicleRepository chronicleRepository,
    ITimeProvider timeProvider,
    ITimeZoneProvider timeZoneProvider,
    IIdGenerator idGenerator)
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var username = cmd.Username.Trim();
        var email = cmd.Email.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cmd.Password))
            return Result<AuthResponseDto>.Conflict("", "Username and password must not be empty.");

        if (string.IsNullOrWhiteSpace(email))
            return Result<AuthResponseDto>.Conflict("", "Email address must not be empty.");

        if (!timeZoneProvider.IsValid(cmd.TimeZone))
            return Result<AuthResponseDto>.Conflict("", "The specified time zone is invalid.");

        var exists = await authStore.UsernameExistsAsync(username, ct);
        if (exists)
            return Result<AuthResponseDto>.Conflict("", "The username is already taken.");

        var userId = idGenerator.New();

        var created = await authStore.CreateUserAsync(
            id: userId,
            email: email,
            username: username,
            password: cmd.Password,
            timeZone: cmd.TimeZone,
            ct: ct);

        if (!created.IsSuccess)
            return Result<AuthResponseDto>.Conflict("", created.Error?.Message ?? "Registration failed.");


        // var journal = new Journal(new JournalId(idGenerator.New()), userId, timeProvider.UtcNow);
        // await chronicleRepository.AddAsync(journal, ct);
        // await chronicleRepository.SaveChangesAsync(ct);

        var dto = new UserDto(userId, username, cmd.TimeZone);
        return Result<AuthResponseDto>.Ok(new AuthResponseDto(dto));
    }
}
