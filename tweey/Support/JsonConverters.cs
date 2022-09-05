namespace Tweey.Support;

class Vector3JsonConverter : JsonConverter<Vector3>
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

        return new Vector3(f1, f2, f3);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) => throw new NotImplementedException();
}
