namespace Tweey.Support;

class CurrentWorldTemplate
{
    BuildingTemplate? buildingTemplate;
    ZoneType? zoneType;

    public bool IsAny => buildingTemplate is { } || zoneType.HasValue;

    public BuildingTemplate? BuildingTemplate
    {
        get => buildingTemplate;
        set { buildingTemplate = value; zoneType = null; }
    }

    public ZoneType? ZoneType
    {
        get => zoneType;
        set { zoneType = value; buildingTemplate = null; }
    }

    public void Clear()
    {
        zoneType = null;
        buildingTemplate = null;
    }
}
