namespace Tweey.Loaders;

class RoomTemplateIn { }

enum RoomRequirementType { Exact, AtLeast }

class RoomRequirementTemplate
{
    public RoomRequirementType Type { get; set; }
    public int Value { get; set; }
    public BuildingTemplate Building { get; set; }
}

class RoomTemplate : ITemplateFileName
{
    public string Name { get; set; }
    public List<RoomRequirementTemplate> Requirements { get; set; }
    public string FileName { get; set; } = null!;
}

readonly struct Room
{
    public Room(World world, IEnumerable<Vector2i> locations, IEnumerable<Entity> buildings)
    {
        Locations = locations.ToList();
        this.buildings = buildings.ToList();
        Template = world.RoomTemplates.GetBestTemplate(this.buildings);
    }

    public IReadOnlyList<Vector2i> Locations { get; }
    readonly List<Entity> buildings;
    public IReadOnlyList<Entity> Buildings => buildings;
    public RoomTemplate? Template { get; }
}

class RoomTemplates : BaseTemplates<RoomTemplateIn, RoomTemplate>
{
    public RoomTemplates(ILoader loader, BuildingTemplates buildingTemplates)
        : base(loader, "Rooms", x => x.FileName!)
    {
        resources = ImmutableSortedDictionary.CreateRange(new KeyValuePair<string, RoomTemplate>[]
        {
            new("bedroom", new()
            {
                Name = "Bedroom",
                Requirements = new()
                {
                    new()
                    {
                        Type = RoomRequirementType.Exact,
                        Value = 1,
                        Building = buildingTemplates["bed"]
                    }
                }
            }),
            new("barracks", new()
            {
                Name="Barracks",
                Requirements = new()
                {
                    new()
                    {
                        Type = RoomRequirementType.AtLeast,
                        Value = 2,
                        Building = buildingTemplates["bed"]
                    }
                }
            })
        });
    }

    public RoomTemplate? GetBestTemplate(IList<Entity> buildings)
    {
        using var buildingCounts = buildings.CountBy(b => b.GetBuildingComponent().Template).ToPooledCollection();
        foreach (var roomTemplate in this)
        {
            var okay = true;
            foreach (var requirement in roomTemplate.Requirements)
            {
                if (buildingCounts.FirstOrDefault(w => w.key == requirement.Building) is { key: { } building, count: var count }
                    && building == requirement.Building
                    && requirement.Type switch
                    {
                        RoomRequirementType.Exact => count == requirement.Value,
                        RoomRequirementType.AtLeast => count >= requirement.Value,
                        _ => throw new NotImplementedException()
                    })
                {
                    continue;
                }

                okay = false;
                break;
            }

            if (okay)
                return roomTemplate;
        }

        return null;
    }
}
