﻿namespace Tweey.Support.AI.HighLevelPlans;

class EatAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity chairEntity;
    private readonly Entity tableEntity;
    private readonly ResourceMarker marker;

    public EatAIHighLevelPlan(World world, Entity mainEntity, Entity chairEntity, Entity tableEntity, ResourceMarker marker)
        : base(world, mainEntity)
    {
        this.chairEntity = chairEntity;
        this.tableEntity = tableEntity;
        this.marker = marker;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        using var foodResources = MainEntity.GetInventoryComponent().Inventory.GetResourceQuantities(marker).ToPooledCollection();
        var done = false;

        if (chairEntity != Entity.Invalid)
        {
            await new WalkAILowLevelPlan(World, MainEntity, chairEntity, true).RunAsync(frameAwaiter);
            chairEntity.GetWorkableComponent().EntityWorking = true;

            // add the temporary food entity
            var chairBox = chairEntity.GetLocationComponent().Box;
            var tableBox = tableEntity.GetLocationComponent().Box;
            Vector2 deltaFood =
                chairBox.Right <= tableBox.Left ? new(1, 0)
                : chairBox.Left >= tableBox.Right ? new(-1, 0)
                : chairBox.Bottom <= tableBox.Top ? new(0, -1)
                : new(0, 1);

            var fakeFoodEntity = EcsCoordinator.CreateEntity();
            fakeFoodEntity.AddLocationComponent(chairEntity.GetLocationComponent().Box.WithOffset(deltaFood));
            fakeFoodEntity.AddRenderableComponent(foodResources[0].Resource.ImageFileName, HigherZOrder: true);
            fakeFoodEntity.AddExpirationComponent() = new(() => done);
        }
        else
            MainEntity.GetVillagerComponent().AddThought(World, World.ThoughtTemplates[ThoughtTemplates.AteOnGround]);

        await new WaitAILowLevelPlan(World, MainEntity, Entity.Invalid, World.RawWorldTime +
            foodResources.Sum(w => w.Quantity) * TimeSpan.FromSeconds(World.Configuration.Data.BaseEatSpeedPerWorldSeconds)).RunAsync(frameAwaiter);
        MainEntity.GetInventoryComponent().Inventory.Remove(marker);
        MainEntity.GetVillagerComponent().Needs.Hunger += foodResources.Sum(w => w.Resource.Nourishment);

        if (chairEntity != Entity.Invalid)
        {
            chairEntity.GetWorkableComponent().Entity = Entity.Invalid;
            chairEntity.GetWorkableComponent().EntityWorking = false;

            if (World.GetRoomAtWorldLocationAsNullable(MainEntity.GetLocationComponent().Box.TopLeft.ToVector2i())?.Template is { } roomTemplate
                && roomTemplate.Thoughts.FirstOrDefault(t => t.Action is RoomThoughtActionType.Eat) is { } roomThought)
            {
                MainEntity.GetVillagerComponent().AddThought(World, roomThought.Thought);
            }
        }
        done = true;
    }
}
