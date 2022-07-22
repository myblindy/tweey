namespace Tweey.Loaders;

public class ConfigurationData
{
    public double GroundStackMaximumWeight { get; set; }
    public double BaseCarryWeight { get; set; }
    public int MaximumGroundDropSpillOverRange { get; set; }
    public double BaseMovementSpeed { get; set; }
    public double BasePickupSpeed { get; set; }
    public double BaseWorkSpeed { get; set; }
    public double BaseHungerMax { get; set; }
    public double BaseHungerPerRealTimeSecond { get; set; }
    public double TicksPerDay { get; set; }
    public double BaseEatSpeed { get; set; }
    public double BaseHungerPercentage { get; set; }
    public double BaseHungerEmergencyPercentage { get; set; }
}

public class Configuration
{
    public ConfigurationData Data { get; }

    public Configuration(ILoader loader)
    {
        var options = Loader.BuildJsonOptions();
        using var stream = new StreamReader(loader.GetJsonData(@"Data/Configuration/config.json")().stream);
        Data = JsonSerializer.Deserialize<ConfigurationData>(stream.ReadToEnd(), options)!;
    }
}
