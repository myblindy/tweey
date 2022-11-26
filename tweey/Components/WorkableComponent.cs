using System.Runtime.CompilerServices;

namespace Tweey.Components;

[EcsComponent]
struct WorkableComponent
{
    public Entity Entity { get; set; }
    public bool EntityWorking { get; set; }

    public List<Bill> Bills { get; } = new();

    public void ClearWorkers() =>
        (Entity, EntityWorking) = (Entity.Invalid, false);

    public WorkableComponent() => ClearWorkers();
}

enum BillAmountType { FixedValue, UntilInStock }

class Bill
{
    public required BuildingProductionLineTemplate ProductionLine { get; init; }
    public required BillAmountType AmountType { get; set; }
    public required int Amount { get; set; }
}