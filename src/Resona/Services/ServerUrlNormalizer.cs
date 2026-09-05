using System;

namespace Resona.Services;

public static class ServerUrlNormalizer
{
    public static bool TryNormalize(string? value, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
            return false;

        var path = uri.AbsolutePath.TrimEnd('/');
        if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase))
            path = string.Empty;

        var builder = new UriBuilder(uri)
        {
            Path = path,
            Query = string.Empty,
            Fragment = string.Empty
        };
        normalizedUrl = builder.Uri.AbsoluteUri.TrimEnd('/');
        return normalizedUrl.Length > 0;
    }
}
