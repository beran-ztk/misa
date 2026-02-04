using Misa.Domain.Features.Users;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IAuthenticationRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> FindByUsernameAndPasswordAsync(string username, string password, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}