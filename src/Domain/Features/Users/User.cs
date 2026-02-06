namespace Misa.Domain.Features.Users;

public sealed class User
{
    private User() { }

    public User(Guid id, string username, string password, string timeZone)
    {
        Id = id;
        Username = username;
        Password = password;
        TimeZone = timeZone;
    }
    
    public Guid Id { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }
    public string TimeZone { get; init; }
}