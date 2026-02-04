using Misa.Application.Common.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Misa.Domain.Features.Users;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public sealed class AuthenticationRepository(DefaultContext context) : IAuthenticationRepository
{
    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default)
        => context.User
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, ct);

    public Task<User?> FindByUsernameAndPasswordAsync(string username, string password, CancellationToken ct = default)
        => context.User
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username && x.Password == password, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        context.User.Add(user);
        await context.SaveChangesAsync(ct);
    }
}
