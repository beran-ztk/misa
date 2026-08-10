using System;

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
    public MusicAnalysisException(MusicAnalysisErrorKind kind, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public MusicAnalysisErrorKind Kind { get; }
}
