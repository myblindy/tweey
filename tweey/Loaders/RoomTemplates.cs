namespace Tweey.Loaders;

enum RoomRequirementType { Exact, AtLeast }

class RoomRequirementTemplateIn
{
    public RoomRequirementType Type { get; set; }
    public int Value { get; set; }
    public string Building { get; set; } = null!;
}

enum RoomThoughtActionType { Eat, Sleep }

class RoomThoughtTemplateIn
{
    public RoomThoughtActionType Action { get; set; }
    public string Thought { get; set; } = null!;
}

class RoomTemplateIn
{
    public string Name { get; set; } = null!;
    public List<RoomRequirementTemplateIn> Requirements { get; set; } = null!;
    public List<RoomThoughtTemplateIn> Thoughts { get; set; } = null!;
}

class RoomRequirementTemplate
{
    public required RoomRequirementType Type { get; set; }
    public required int Value { get; set; }
    public required BuildingTemplate Building { get; set; }
}

class RoomThoughtTemplate
{
    public required RoomThoughtActionType Action { get; set; }
    public required ThoughtTemplate Thought { get; set; }
}

class RoomTemplate : ITemplateFileName
{
    public required string Name { get; set; }
    public required RoomRequirementTemplate[] Requirements { get; set; }
    public required RoomThoughtTemplate[] Thoughts { get; set; }
    public string FileName { get; set; } = null!;
}

readonly struct Room
{
    public Room(RoomTemplates roomTemplates, IEnumerable<Vector2i> locations, IEnumerable<Entity> buildings)
    {
        Locations = locations.ToList();
        this.buildings = buildings.ToList();
        Template = roomTemplates.GetBestTemplate(this.buildings);
    }

    public IReadOnlyList<Vector2i> Locations { get; }
    readonly List<Entity> buildings;
    public IReadOnlyList<Entity> Buildings => buildings;
    public RoomTemplate? Template { get; }
}

class RoomTemplates : BaseTemplates<RoomTemplateIn, RoomTemplate>
{
    public const string DiningRoomFileName = "dining-room";
    public const string BedroomFileName = "bed-room";
    public const string BarracksFileName = "barracks";

    public RoomTemplates(ILoader loader, BuildingTemplates buildingTemplates, ThoughtTemplates thoughtTemplates)
        : base(loader, "Rooms", x => x.FileName!, (buildingTemplates, thoughtTemplates))
    {
    }

    public RoomTemplate? GetBestTemplate(IList<Entity> buildings)
    {
        using var buildingCounts = buildings.Where(b => b.GetBuildingComponent().IsBuilt)
            .CountBy(b => b.GetBuildingComponent().Template).ToPooledCollection();

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

            if (okay && !buildingCounts.Select(w => w.key).Except(roomTemplate.Requirements.Select(w => w.Building)).Any())
                return roomTemplate;
        }

        return null;
    }
}
