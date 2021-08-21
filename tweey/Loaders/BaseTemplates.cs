namespace Tweey.Loaders
{
    abstract class BaseTemplates<TIn, TVal> : IEnumerable<string>
    {
        readonly ImmutableDictionary<string, TVal> resources;

        public BaseTemplates(ILoader loader, IMapper mapper, string subFolder, Func<TVal, string> keySelector)
        {
            var options = Loader.BuildJsonOptions();
            resources = loader.GetAllJsonData($@"Data/{subFolder}").Values
                .Select(sgen =>
                {
                    using var stream = new StreamReader(sgen());
                    var @in = JsonSerializer.Deserialize<TIn>(stream.ReadToEnd(), options)!;
                    return mapper is not null ? mapper.Map<TVal>(@in) : (TVal)(object)@in;
                })
                .ToImmutableDictionary(keySelector, StringComparer.CurrentCultureIgnoreCase);
        }

        public IEnumerator<string> GetEnumerator() => resources.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TVal this[string key] => resources[key];
    }
}
