namespace tweey.Loaders
{
    enum BuildingType { WorkPlace, Storage }

    class BuildingTemplate
    {
        public string Name { get; set; }
        public BuildingType Type { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    class BuildingTemplates : BaseTemplates<BuildingTemplate>
    {
        public BuildingTemplates(ILoader loader) : base(loader, "Buildings", x => x.Name)
        {
        }
    }
}
