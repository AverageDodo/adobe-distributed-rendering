using System.Text.Json;
using System.Text.Json.Serialization;

namespace DistributedRendering.AME.Shared.Util;

public static class JsonConfig
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = false,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
        Converters = { new DirectoryInfoJsonConverter(), new FileInfoJsonConverter(), new JsonStringEnumConverter(), new TimeSpanJsonConverter() }
    };
}

public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var timeString = reader.GetString();

        return TimeSpan.TryParse(timeString, out TimeSpan timeSpan) ? timeSpan : TimeSpan.Zero;
    }
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("c"));
    }
}

public class DirectoryInfoJsonConverter : JsonConverter<DirectoryInfo>
{
    public override DirectoryInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var pathString = reader.GetString();

        if (string.IsNullOrWhiteSpace(pathString) || !Path.EndsInDirectorySeparator(pathString))
            return null;

        return new DirectoryInfo(pathString);
    }

    public override void Write(Utf8JsonWriter writer, DirectoryInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.FullName);
    }
}

public class FileInfoJsonConverter : JsonConverter<FileInfo>
{
    public override FileInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var pathString = reader.GetString();

        if (string.IsNullOrWhiteSpace(pathString) || Path.EndsInDirectorySeparator(pathString))
            return null;

        return new FileInfo(pathString);
    }

    public override void Write(Utf8JsonWriter writer, FileInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.FullName);
    }
}