using System.Diagnostics.CodeAnalysis;

namespace Twee.Core.Ecs;

public struct EcsMapping
{
    const int minimumEntityCount = 2000;

    readonly int componentCount;
    int[] data;
    int entityCount;

    public EcsMapping(int componentCount)
    {
        this.componentCount = componentCount;
        ResizeData(minimumEntityCount);
    }

    [MemberNotNull(nameof(data))]
    void ResizeData(int entityCount)
    {
        var newData = new int[componentCount * entityCount];

        data?.CopyTo(newData, 0);
        Array.Fill(newData, -1, data?.Length ?? 0, newData.Length - (data?.Length ?? 0));

        data = newData;
        this.entityCount = entityCount;
    }

    public void EnsureEntityExists(int entityId)
    {
        if (entityId >= entityCount)
        {
            int newCount = entityCount;
            do
            {
                newCount = (int)(newCount * 1.5);
            } while (newCount <= entityId);

            ResizeData(newCount);
        }
    }

    public int this[int entityId, int componentId]
    {
        get => data[componentCount * entityId + componentId];
        set => data[componentCount * entityId + componentId] = value;
    }
}
