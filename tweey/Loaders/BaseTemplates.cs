using MoreLinq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace tweey.Loaders
{
    abstract class BaseTemplates<T> : IEnumerable<string>
    {
        readonly ImmutableDictionary<string, T> data;

        public BaseTemplates(ILoader loader, string subFolder, Func<T, string> keySelector)
        {
            var options = Loader.BuildJsonOptions();
            data = loader.GetAllJsonData($@"Data/{subFolder}").Values
                .Select(sgen =>
                {
                    using var stream = new StreamReader(sgen());
                    return JsonSerializer.Deserialize<T>(stream.ReadToEnd(), options);
                })
                .ToImmutableDictionary(keySelector, StringComparer.CurrentCultureIgnoreCase);
        }

        public IEnumerator<string> GetEnumerator() => data.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T this[string key] => data[key];
    }
}
