using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Features.Users;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public sealed class AuthenticationRepository(DefaultContext context) : IAuthenticationRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default)
        => context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, ct);

    public Task<User?> FindByUsernameAndPasswordAsync(string username, string password, CancellationToken ct = default)
        => context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username && x.Password == password, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(ct);
    }
}
