namespace Tweey.Loaders
{
    public class ConfigurationData
    {
        public double GroundStackMaximumWeight { get; set; }
        public double BaseCarryWeight { get; set; }
        public int MaximumGroundDropSpillOverRange { get; set; }
        public double BaseMovementSpeed { get; set; }
        public double BasePickupSpeed { get; set; }
    }

    public class Configuration
    {
        public ConfigurationData Data { get; }

        public Configuration(ILoader loader)
        {
            var options = Loader.BuildJsonOptions();
            using var stream = new StreamReader(loader.GetJsonData(@"Data/Configuration/config.json")());
            Data = JsonSerializer.Deserialize<ConfigurationData>(stream.ReadToEnd(), options)!;
        }
    }
}
