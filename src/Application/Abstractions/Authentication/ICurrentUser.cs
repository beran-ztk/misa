namespace Misa.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    string Id { get; }
    string Timezone { get; }
}
