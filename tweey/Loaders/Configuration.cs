namespace Tweey.Loaders;

class ConfigurationData
{
    public double GroundStackMaximumWeight { get; set; }
    public double BaseCarryWeight { get; set; }
    public int MaximumGroundDropSpillOverRange { get; set; }
    public double BaseMovementSpeed { get; set; }
    public double BasePickupSpeed { get; set; }
    public double BaseWorkSpeed { get; set; }
    public double BaseHarvestSpeed { get; set; }
    public double BasePlantSpeed { get; set; }
    public double BaseHungerMax { get; set; }
    public double BaseHungerPerRealTimeSecond { get; set; }
    public double TicksPerDay { get; set; }
    public double BaseEatSpeed { get; set; }
    public double BaseHungerPercentage { get; set; }
    public double BaseHungerEmergencyPercentage { get; set; }
    public Vector3 ZoneGrowColor { get; set; }
    public Vector3 ZoneHarvestColor { get; set; }
    public Vector3 ZoneErrorColor { get; set; }
    public Vector3 MidDayColor { get; set; }
    public Vector3 MidNightColor { get; set; }
}

class Configuration
{
    public ConfigurationData Data { get; }

    public Configuration(ILoader loader)
    {
        using var stream = new StreamReader(loader.GetJsonData(@"Data/Configuration/config.json")().stream!);
        Data = JsonSerializer.Deserialize<ConfigurationData>(stream.ReadToEnd(), Loader.BuildJsonOptions())!;
    }
}
