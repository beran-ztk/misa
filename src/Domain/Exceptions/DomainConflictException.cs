namespace Misa.Domain.Exceptions;

public sealed class DomainConflictException : Exception
{
    public string Code { get; }

    public DomainConflictException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
