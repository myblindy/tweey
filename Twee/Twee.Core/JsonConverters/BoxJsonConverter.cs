using System.Text.Json;
using System.Text.Json.Serialization;

namespace Twee.Core.JsonConverters;

public class Box2JsonConverter : JsonConverter<Box2>
{
    public override Box2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read())
            throw new JsonException();

        reader.Read();
        var f1 = reader.GetSingle();
        reader.Read();
        var f2 = reader.GetSingle();
        reader.Read();
        var f3 = reader.GetSingle();
        reader.Read();
        var f4 = reader.GetSingle();

        if (!reader.Read())
            throw new JsonException();

        return new() { TopLeft = new(f1, f2), BottomRight = new(f3, f4) };
    }

    public override void Write(Utf8JsonWriter writer, Box2 value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Top);
        writer.WriteNumberValue(value.Left);
        writer.WriteNumberValue(value.Bottom);
        writer.WriteNumberValue(value.Right);
    }
}
