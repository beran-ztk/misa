using System;
using System.Net;

namespace Resona.Services;

public enum MusicAnalysisErrorKind
{
    ConnectionError,
    Timeout,
    ServerError,
    InvalidResponse,
    FileError,
    Cancelled
}

public sealed class MusicAnalysisException : Exception
{
    public MusicAnalysisException(
        MusicAnalysisErrorKind kind,
        string message,
        Exception? innerException = null,
        HttpStatusCode? statusCode = null)
        : base(message, innerException)
    {
        Kind = kind;
        StatusCode = statusCode;
    }

    public MusicAnalysisErrorKind Kind { get; }
    public HttpStatusCode? StatusCode { get; }
}
