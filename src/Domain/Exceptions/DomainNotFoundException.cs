namespace Misa.Domain.Exceptions;

public sealed class DomainNotFoundException : Exception
{
    public string Code { get; }

    public DomainNotFoundException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
