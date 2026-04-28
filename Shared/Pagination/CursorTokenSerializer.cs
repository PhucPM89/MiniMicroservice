using Shared.Exceptions;
using System.Text;
using System.Text.Json;

namespace Shared.Pagination;

public static class CursorTokenSerializer
{
    public static string Encode(TimestampCursor cursor)
    {
        var json = JsonSerializer.Serialize(cursor);
        var bytes = Encoding.UTF8.GetBytes(json);
        return ToBase64Url(bytes);
    }

    public static TimestampCursor? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(FromBase64Url(cursor));
            var result = JsonSerializer.Deserialize<TimestampCursor>(json);
            if (result is null || result.TimestampUtc == default || result.LastId == Guid.Empty)
            {
                throw new ValidationException(["The cursor is invalid."], "Pagination failed.");
            }

            return result;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch
        {
            throw new ValidationException(["The cursor is invalid."], "Pagination failed.");
        }
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        var normalized = value
            .Replace('-', '+')
            .Replace('_', '/');

        return Convert.FromBase64String(normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '='));
    }
}
