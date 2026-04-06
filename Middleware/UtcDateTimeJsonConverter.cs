using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Ensures all DateTime values are serialized with the 'Z' suffix (UTC)
/// and deserialized as DateTimeKind.Utc. This prevents timezone issues
/// when EF Core returns DateTimeKind.Unspecified from SQL Server.
/// </summary>
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dt = reader.GetDateTime();
        return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Force UTC kind so System.Text.Json appends the 'Z' suffix
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc);
    }
}
