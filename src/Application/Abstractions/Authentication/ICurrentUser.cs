namespace Misa.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    string UserId { get; }
}
