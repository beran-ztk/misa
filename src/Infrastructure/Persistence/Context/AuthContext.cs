using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Misa.Infrastructure.Persistence.Models;

namespace Misa.Infrastructure.Persistence.Context;

public sealed class AuthContext(DbContextOptions<AuthContext> options) : IdentityDbContext<User>(options);