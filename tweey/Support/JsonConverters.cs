namespace Tweey.Support;

class ResourceJsonConverter : JsonConverter<Resource>
{
    public override Resource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, Resource value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.FileName);
    }
}
