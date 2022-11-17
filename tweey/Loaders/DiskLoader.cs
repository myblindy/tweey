namespace Tweey.Loaders;

public interface ILoader
{
    ImmutableDictionary<string, Func<(Stream? stream, string fileName)>> GetAllJsonData(string root);
    Func<(Stream? stream, string fileName)> GetJsonData(string path);
}

public class DiskLoader : ILoader, IDisposable
{
    public VFSReader VFS { get; } = new("Data.bin");

    private DiskLoader()
    {
    }

    public ImmutableDictionary<string, Func<(Stream? stream, string fileName)>> GetAllJsonData(string root) =>
        VFS.EnumerateFiles(root, SearchOption.AllDirectories)
            .Where(path =>
                Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)
                && !Path.GetFileName(path).Equals("_schema.json", StringComparison.OrdinalIgnoreCase))
            .ToImmutableDictionary(path => path, GetJsonData);

    public Func<(Stream? stream, string fileName)> GetJsonData(string path) =>
        () => (VFS.OpenRead(path), path);

    public static DiskLoader Instance { get; } = new();

    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // managed
            }

            // unmanaged
            ((IDisposable)VFS).Dispose();
            disposedValue = true;
        }
    }

    ~DiskLoader()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    void IDisposable.Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public static class Loader
{
    public static JsonSerializerOptions BuildJsonOptions() =>
        new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Converters = { new JsonStringEnumConverter(), new ValueTupleJsonConverterFactory(), new EntityJsonConverter(), new ResourceJsonConverter(),
                new Vector3JsonConverter(), new Vector2JsonConverter(), new Box2JsonConverter() },
        };
}
