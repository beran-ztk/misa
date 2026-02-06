using Misa.Domain.Features.Users;

namespace Misa.Application.Abstractions.Persistence;

public interface IAuthenticationRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> FindByUsernameAndPasswordAsync(string username, string password, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}