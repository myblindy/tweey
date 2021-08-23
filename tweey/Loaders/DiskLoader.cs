using System.Text.Json.Serialization;

namespace Tweey.Loaders
{
    public interface ILoader
    {
        ImmutableDictionary<string, Func<Stream>> GetAllJsonData(string root);
        Func<Stream> GetJsonData(string path);
    }

    public class DiskLoader : ILoader
    {
        public ImmutableDictionary<string, Func<Stream>> GetAllJsonData(string root) =>
            Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).Equals("_schema.json", StringComparison.OrdinalIgnoreCase))
                .ToImmutableDictionary(path => path, GetJsonData);

        public Func<Stream> GetJsonData(string path) => () => File.OpenRead(path);

        public static DiskLoader Instance { get; } = new();
    }

    public static class Loader
    {
        public static JsonSerializerOptions BuildJsonOptions() =>
            new()
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            };
    }
}
