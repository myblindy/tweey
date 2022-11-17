﻿using System.Runtime.CompilerServices;

namespace Tweey.Components;

[EcsComponent]
struct WorkableComponent
{
    public WorkableComponent() =>
        ResizeSlots(0);

    [MemberNotNull(nameof(WorkerSlots))]
    public void ResizeSlots(int slots)
    {
        if (slots == 0)
            WorkerSlots = Array.Empty<WorkerSlot>();
        else
        {
            var extraNewSlots = slots - (WorkerSlots?.Length ?? 0);
            WorkerSlots = new WorkerSlot[slots];

            while (extraNewSlots-- > 0)
                WorkerSlots[extraNewSlots] = new();
        }
    }

    public ref WorkerSlot GetEmptyWorkerSlot()
    {
        for (int i = 0; i < WorkerSlots.Length; i++)
            if (WorkerSlots[i].Entity == Entity.Invalid)
                return ref WorkerSlots[i];

        return ref Unsafe.NullRef<WorkerSlot>();
    }

    public ref WorkerSlot GetAssignedWorkerSlot(Entity worker)
    {
        for (int i = 0; i < WorkerSlots.Length; i++)
            if (WorkerSlots[i].Entity == worker)
                return ref WorkerSlots[i];

        return ref Unsafe.NullRef<WorkerSlot>();
    }

    public WorkerSlot[] WorkerSlots { get; private set; }
}

struct WorkerSlot
{
    public WorkerSlot() { }

    public void Clear() =>
        (Entity, EntityWorking) = (Entity.Invalid, default);

    public Entity Entity { get; set; } = Entity.Invalid;
    public bool EntityWorking { get; set; }
}