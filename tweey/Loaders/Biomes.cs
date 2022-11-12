namespace Tweey.Loaders;

class BiomeTreeSpawnIn
{
    public string Name { get; set; } = null!;
    public double Chance { get; set; }
}

class BiomeIn
{
    public string Name { get; set; } = null!;
    public string? TileName { get; set; }
    public double MinHeight { get; set; }
    public double MinMoisture { get; set; }
    public double MinHeat { get; set; }
    public List<BiomeTreeSpawnIn>? Trees { get; set; }
}

class Biome
{
    public required string Name { get; init; }
    public required string TileName { get; init; }
    public required double MinHeight { get; set; }
    public required double MinMoisture { get; set; }
    public required double MinHeat { get; set; }
    public required (TreeTemplate template, double chance)[] Trees { get; init; }
}

class Biomes : IReadOnlyDictionary<string, Biome>
{
    readonly Dictionary<string, Biome> biomes;

    public Biomes(ILoader loader, TreeTemplates treeTemplates)
    {
        using var stream = new StreamReader(loader.GetJsonData(@"Data/Biomes/biomes.json")().stream!);
        biomes = JsonSerializer.Deserialize<BiomeContainerIn>(stream.ReadToEnd(), Loader.BuildJsonOptions())!.Biomes
            .ToDictionary(w => w.Name, w => GlobalMapper.Mapper.Map(w, treeTemplates));
    }

    class BiomeContainerIn
    {
        public List<BiomeIn> Biomes { get; set; } = null!;
    }

    public Biome this[string key] => biomes[key];
    public Biome this[int index] => biomes.ElementAt(index).Value;

    public IEnumerable<string> Keys => biomes.Keys;

    public IEnumerable<Biome> Values => biomes.Values;

    public int Count => biomes.Count;

    public bool ContainsKey(string key) => biomes.ContainsKey(key);
    public IEnumerator<KeyValuePair<string, Biome>> GetEnumerator() => biomes.GetEnumerator();
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out Biome value) =>
        biomes.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
