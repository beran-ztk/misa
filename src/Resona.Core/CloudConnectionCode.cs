using System.Text;
using System.Text.Json;

namespace Resona.Core;

public sealed record CloudConnectionPayload(
    int SchemaVersion,
    string ServerUrl,
    string UserId,
    string DeviceId,
    string DeviceKey);

public static class CloudConnectionCode
{
    public const int CurrentSchemaVersion = 1;
    private const string Prefix = "resona-cloud:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Encode(CloudConnectionPayload payload)
    {
        Validate(payload);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static CloudConnectionPayload Decode(string value)
    {
        value = value.Trim();
        if (!value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("This is not a Resona cloud connection code.");

        var encoded = value[Prefix.Length..].Replace('-', '+').Replace('_', '/');
        encoded = encoded.PadRight(encoded.Length + (4 - encoded.Length % 4) % 4, '=');
        try
        {
            var payload = JsonSerializer.Deserialize<CloudConnectionPayload>(
                              Encoding.UTF8.GetString(Convert.FromBase64String(encoded)),
                              JsonOptions)
                          ?? throw new InvalidDataException("The connection code is empty.");
            Validate(payload);
            return payload;
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("The connection code is damaged.", exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The connection code is damaged.", exception);
        }
    }

    private static void Validate(CloudConnectionPayload payload)
    {
        if (payload.SchemaVersion != CurrentSchemaVersion
            || !Uri.TryCreate(payload.ServerUrl, UriKind.Absolute, out var serverUri)
            || serverUri.Scheme is not ("http" or "https")
            || !Guid.TryParse(payload.UserId, out _)
            || !Guid.TryParse(payload.DeviceId, out _))
            throw new InvalidDataException("The cloud connection data is invalid.");
        try
        {
            if (Convert.FromBase64String(payload.DeviceKey).Length != 32)
                throw new InvalidDataException("The cloud device key is invalid.");
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("The cloud device key is invalid.", exception);
        }
    }
}
