namespace Misa.Domain.Exceptions;

public sealed class DomainValidationException : Exception
{
    public string Field { get; }
    public string Code { get; }

    public DomainValidationException(string field, string code, string message)
        : base(message)
    {
        Field = field;
        Code = code;
    }
}
