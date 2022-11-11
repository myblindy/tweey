using System.Text.Json.Serialization;
using System.Text.Json;

namespace Twee.Core.Ecs;

public class EntityJsonConverter : JsonConverter<Entity>
{
    public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read())
            throw new JsonException();
        return new() { ID = reader.GetInt32() };
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.ID);
}
