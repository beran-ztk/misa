using Misa.Application.Abstractions.Ids;

namespace Misa.Infrastructure.Services.Ids;

public sealed class GuidIdGenerator : IIdGenerator
{
    public Guid New() => Guid.NewGuid();
}