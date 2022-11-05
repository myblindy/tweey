using System.Text.Json.Serialization;
using System.Text.Json;

namespace Tweey.Support;

public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartArray)
            throw new JsonException();

        reader.Read();
        var f1 = reader.GetSingle();
        reader.Read();
        var f2 = reader.GetSingle();
        reader.Read();
        var f3 = reader.GetSingle();

        reader.Read();
        if (reader.TokenType is not JsonTokenType.EndArray)
            throw new JsonException();

        return new(f1, f2, f3);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}

public class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartArray)
            throw new JsonException();

        reader.Read();
        var f1 = reader.GetSingle();
        reader.Read();
        var f2 = reader.GetSingle();

        reader.Read();
        if (reader.TokenType is not JsonTokenType.EndArray)
            throw new JsonException();

        return new(f1, f2);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}
