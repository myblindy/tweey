namespace Tweey.Loaders;

public interface ITemplateFileName
{
    string FileName { get; set; }
}

public abstract class BaseTemplates<TIn, TVal> : IEnumerable<string> where TVal : ITemplateFileName
{
    readonly ImmutableDictionary<string, TVal> resources;

    protected BaseTemplates(ILoader loader, string subFolder, Func<TVal, string> keySelector, object? mapperParameter = null)
    {
        var options = Loader.BuildJsonOptions();
        resources = loader.GetAllJsonData($@"Data/{subFolder}").Values
            .Select(sgen =>
            {
                var (stream, fileName) = sgen();
                using var streamReader = new StreamReader(stream!);
                var @in = JsonSerializer.Deserialize<TIn>(streamReader.ReadToEnd(), options)!;
                var result = GlobalMapper.Mapper.Map<TVal>(@in, mapperParameter) ?? (TVal)(object)@in;
                result.FileName = Path.GetFileNameWithoutExtension(fileName);
                return result;
            })
            .ToImmutableDictionary(keySelector, StringComparer.CurrentCultureIgnoreCase);
    }

    public IEnumerator<string> GetEnumerator() => resources.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public TVal this[string key] => resources[key];
}
