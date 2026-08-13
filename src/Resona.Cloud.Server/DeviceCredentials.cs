using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

namespace Resona.Cloud.Server;

public sealed record DeviceCredentials(Guid UserId, Guid DeviceId, byte[] DeviceKeyHash);

public static class DeviceCredentialsReader
{
    public static bool TryRead(
        IHeaderDictionary headers,
        out DeviceCredentials credentials,
        out string error)
    {
        credentials = null!;
        if (!TrySingle(headers, "X-Resona-User-Id", out var userIdText)
            || !Guid.TryParse(userIdText, out var userId))
            return Fail("A valid X-Resona-User-Id header is required.", out error);
        if (!TrySingle(headers, "X-Resona-Device-Id", out var deviceIdText)
            || !Guid.TryParse(deviceIdText, out var deviceId))
            return Fail("A valid X-Resona-Device-Id header is required.", out error);
        if (!TrySingle(headers, "Authorization", out var authorization)
            || !authorization.StartsWith("Device ", StringComparison.OrdinalIgnoreCase))
            return Fail("Authorization must use the Device scheme.", out error);

        byte[] deviceKey;
        try
        {
            deviceKey = Convert.FromBase64String(authorization[7..].Trim());
        }
        catch (FormatException)
        {
            return Fail("Device key is not valid base64.", out error);
        }
        if (deviceKey.Length != 32)
            return Fail("Device key must contain 32 bytes.", out error);

        credentials = new DeviceCredentials(userId, deviceId, SHA256.HashData(deviceKey));
        error = string.Empty;
        CryptographicOperations.ZeroMemory(deviceKey);
        return true;
    }

    private static bool TrySingle(IHeaderDictionary headers, string name, out string value)
    {
        value = string.Empty;
        return headers.TryGetValue(name, out StringValues values)
               && values.Count == 1
               && !string.IsNullOrWhiteSpace(value = values[0]!);
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }
}
