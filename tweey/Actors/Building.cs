namespace Tweey.Actors;

public class Building : BuildingTemplate
{
    public List<ActiveProductionLine> ActiveProductionLines { get; } = new();
    public AssignedWorker[] AssignedWorkers { get; private set; } = Array.Empty<AssignedWorker>();
    public Building() => Inventory = new() { Building = this };

    public bool IsBuilt { get; set; }
    public ResourceBucket Inventory { get; }

    public static Building FromTemplate(BuildingTemplate template, Vector2 location, bool built)
    {
        var b = GlobalMapper.Mapper.Map(template);
        b.Location = location;
        b.IsBuilt = built;

        if (!built)
        {
            // one spot to work to build this thing
            b.AssignedWorkers = new AssignedWorker[] { new() };
        }
        else
        {
            b.FinishBuilding();
        }

        return b;
    }

    public void FinishBuilding()
    {
        IsBuilt = true;

        AssignedWorkers = new AssignedWorker[MaxWorkersAmount];
        for (var idx = 0; idx < MaxWorkersAmount; ++idx)
            AssignedWorkers[idx] = new();
    }

    public AssignedWorker? GetAssignedWorkerSlot(Villager villager) =>
        AssignedWorkers.FirstOrDefault(w => w.Villager == villager);

    public AssignedWorker? GetEmptyAssignedWorkerSlot() =>
        AssignedWorkers.FirstOrDefault(w => w.Villager is null);
}

public class AssignedWorker
{
    public Villager? Villager { get; set; }
    public bool VillagerWorking { get; set; }

    ActiveProductionLine? activeProductionLine;
    public ActiveProductionLine? ActiveProductionLine
    {
        get => activeProductionLine;
        set { activeProductionLine = value; if (value is not null) ActiveProductionLineWorkTicksLeft = ActiveProductionLine!.ProductionLine!.WorkTicks; }
    }
    public int ActiveProductionLineWorkTicksLeft { get; set; }
}

public enum ActiveProductionLineType { FixedAmount, UntilStock }

public class ActiveProductionLine
{
    public BuildingProductionLineTemplate? ProductionLine { get; set; }
    public ActiveProductionLineType Type { get; set; }
    public int OutputTarget { get; set; }
}