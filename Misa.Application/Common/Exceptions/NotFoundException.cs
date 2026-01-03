namespace Misa.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public static NotFoundException For(string resourceName, object key)
        => new($"{resourceName} '{key}' was not found.");
    
    public NotFoundException(string message) : base(message) {}
}