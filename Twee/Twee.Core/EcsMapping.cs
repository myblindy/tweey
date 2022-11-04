using System.Diagnostics.CodeAnalysis;

namespace Twee.Core;

public struct EcsMapping
{
    const int minimumEntityCount = 2000;

    readonly int componentCount;
    int[] data;

    int EntityCount => (data?.Length ?? 0) / componentCount;

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
    }

    void EnsureEntityExists(int entityId)
    {
        if (entityId >= EntityCount)
        {
            int newCount = EntityCount;
            do
            {
                newCount = (int)(newCount * 1.5);
            } while (newCount <= EntityCount);

            ResizeData(newCount);
        }
    }

    public int this[int entityId, int componentId]
    {
        get
        {
            EnsureEntityExists(entityId);
            return data[componentCount * entityId + componentId];
        }
        set
        {
            EnsureEntityExists(entityId);
            data[componentCount * entityId + componentId] = value;
        }
    }
}
