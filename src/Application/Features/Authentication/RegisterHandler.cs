using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Authentication;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Authentication;
using Misa.Domain.Exceptions;
using Misa.Domain.Features.Audit;

namespace Misa.Application.Features.Authentication;

public sealed record RegisterCommand(string Email, string Username, string Password, string TimeZone);

public sealed class RegisterHandler(
    IIdentityAuthStore authStore,
    ITimeZoneProvider timeZoneProvider,
    IIdGenerator idGenerator)
{
    public async Task Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var username = cmd.Username.Trim();
        var email = cmd.Email.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cmd.Password))
            throw new DomainValidationException("username.password", "", "Username and password must not be empty.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainValidationException("email", "", "Email must not be empty.");

        if (!timeZoneProvider.IsValid(cmd.TimeZone))
            throw new DomainConflictException("", "The specified time zone is invalid.");

        var exists = await authStore.UsernameExistsAsync(username, ct);
        if (exists)
            throw new DomainConflictException("", "The specified username already exists.");

        var userId = idGenerator.New();

        var created = await authStore.CreateUserAsync(
            id: userId,
            email: email,
            username: username,
            password: cmd.Password,
            timeZone: cmd.TimeZone,
            ct: ct);

        if (!created.IsSuccess)
            throw new DomainConflictException("", created.Error?.Message ?? "Registration failed.");
    }
}
