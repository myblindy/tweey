using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tweey.Loaders
{
    interface ILoader
    {
        ImmutableDictionary<string, Func<Stream>> GetAllJsonData(string root);
    }

    class DiskLoader : ILoader
    {
        public ImmutableDictionary<string, Func<Stream>> GetAllJsonData(string root) =>
            Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).Equals("_schema.json", StringComparison.OrdinalIgnoreCase))
                .ToImmutableDictionary(path => path, path => new Func<Stream>(() => File.OpenRead(path)));

        public static DiskLoader Instance { get; } = new();
    }

    static class Loader
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
