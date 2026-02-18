using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Features.Authentication;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Users;

namespace Misa.Application.Features.Authentication;
public sealed record RegisterCommand(string Username, string Password, string TimeZone);

public sealed class RegisterHandler(
    IAuthenticationRepository authenticationRepository, 
    IChronicleRepository chronicleRepository,
    ITimeProvider timeProvider,
    ITimeZoneProvider timeZoneProvider, 
    IIdGenerator idGenerator)
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var username = cmd.Username.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cmd.Password))
            return Result<AuthResponseDto>.Conflict("","Username und Password dürfen nicht leer sein.");

        var existing = await authenticationRepository.FindByUsernameAsync(username, ct);
        if (existing is not null)
            return Result<AuthResponseDto>.Conflict("","Username ist bereits vergeben.");

        if (!timeZoneProvider.IsValid(cmd.TimeZone))
            return Result<AuthResponseDto>.Conflict("","Ungültige Zeitzone.");
        
        var user = new User(idGenerator.New(), username, cmd.Password, cmd.TimeZone);
        await authenticationRepository.AddAsync(user, ct);
        await authenticationRepository.SaveChangesAsync(ct);

        var journal = new Journal(new JournalId(idGenerator.New()), user.Id, timeProvider.UtcNow);
        await chronicleRepository.AddAsync(journal, ct);
        await chronicleRepository.SaveChangesAsync(ct);

        var dto = new UserDto(user.Id, user.Username, user.TimeZone);
        return Result<AuthResponseDto>.Ok(new AuthResponseDto(dto));
    }
}