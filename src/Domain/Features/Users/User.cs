namespace Misa.Domain.Features.Users;

public sealed class User
{
    private User() { }

    public User(string username, string password, string timeZone)
    {
        Username = username;
        Password = password;
        TimeZone = timeZone;
    }
    
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Username { get; init; }
    public string Password { get; init; }
    public string TimeZone { get; init; }
}