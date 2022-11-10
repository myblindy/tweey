namespace Tweey.Systems;

partial class RenderSystem
{
    void InitializeGui()
    {
        Vector4 panelBackgroundColor = new(.1f, .1f, .1f, 1);
        var descriptionColor = new Vector4(.8f, .8f, .8f, 1);
        var highlightColor = Colors4.Aqua;
        var defaultFontSize = 18;

        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                BackgroundColor = panelBackgroundColor,
                Children =
                {
                    new LabelView { Text = () => $"Time: {world.WorldTimeString}" },
                    new StackView(StackType.Horizontal)
                    {
                        Children=
                        {
                            new ButtonView
                            {
                                Child = new LabelView { Text = () => "0x" },
                                IsChecked = () => world.TimeSpeedUp == 0,
                                Clicked = () => world.TimeSpeedUp = 0,
                            },
                            new ButtonView
                            {
                                Child = new LabelView { Text = () => "1x" },
                                IsChecked = () => world.TimeSpeedUp == 1,
                                Clicked = () => world.TimeSpeedUp = 1,
                            },
                            new ButtonView
                            {
                                Child = new LabelView { Text = () => "2x" },
                                IsChecked = () => world.TimeSpeedUp == 2,
                                Clicked = () => world.TimeSpeedUp = 2,
                            },
                            new ButtonView
                            {
                                Child = new LabelView { Text = () => "4x" },
                                IsChecked = () => world.TimeSpeedUp == 4,
                                Clicked = () => world.TimeSpeedUp = 4,
                            },
                            new ButtonView
                            {
                                Child = new LabelView { Text = () => "8x" },
                                IsChecked = () => world.TimeSpeedUp == 8,
                                Clicked = () => world.TimeSpeedUp = 8,
                            },
                        }
                    }
                }
            }, Anchor.BottomRight));

        static string? getEntityDescription(Entity entity) =>
            EcsCoordinator.HasVillagerComponent(entity) ? "Villager"
            : EcsCoordinator.HasResourceComponent(entity) ? "Resource"
            : EcsCoordinator.HasBuildingComponent(entity) ? !EcsCoordinator.GetBuildingComponent(entity).IsBuilt ? "Building Site" : "Building"
            : EcsCoordinator.HasTreeComponent(entity) ? "Tree"
            : throw new NotImplementedException();

        View getResourceRowView(bool labor, Resource? resource, Func<double> quantity) =>
            new StackView(StackType.Horizontal)
            {
                Children =
                {
                    new LabelView
                    {
                        Text = () => quantity().ToString(),
                        FontSize = defaultFontSize,
                        MinWidth = () => 50,
                        Margin = new(10,0,10,0),
                        HorizontalTextAlignment = HorizontalAlignment.Right,
                        ForegroundColor = () => highlightColor
                    },
                    new ImageView
                    {
                        Source = () => labor ? "Data/Resources/labor.png" : resource!.ImageFileName,
                        InheritParentSize = true
                    },
                    new LabelView
                    {
                        Text = () => $" {(labor ? "Work" : resource!.Name)}",
                        FontSize = defaultFontSize,
                        ForegroundColor = () => descriptionColor
                    }
                }
            };

        // selection box
        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                BackgroundColor = panelBackgroundColor,
                IsVisible = () => world.SelectedEntity.HasValue,
                Children =
                {
                    // header
                    new StackView(StackType.Horizontal)
                    {
                        Children =
                        {
                            new ImageView
                            {
                                Source = () => EcsCoordinator.GetRenderableComponent(world.SelectedEntity!.Value).AtlasEntryName,
                                InheritParentSize = true,
                            },
                            new LabelView
                            {
                                Text = () => getEntityDescription(world.SelectedEntity!.Value),
                                FontSize = 30,
                                ForegroundColor = () => descriptionColor
                            },
                            new LabelView
                            {
                                Text = () => EcsCoordinator.GetIdentityComponent(world.SelectedEntity!.Value).Name,
                                FontSize = 30,
                                Padding = new(10, 0, 0, 0),
                                ForegroundColor = () => highlightColor
                            },
                        }
                    },

                    // building site details
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => EcsCoordinator.HasBuildingComponent(world.SelectedEntity!.Value)
                            && !EcsCoordinator.GetBuildingComponent(world.SelectedEntity!.Value).IsBuilt,
                        Children =
                        {
                            new LabelView
                            {
                                Padding = new(25, 15, 0, 0),
                                FontSize = defaultFontSize,
                                Text = () => "Required:"
                            },
                            new RepeaterView<ResourceQuantity>
                            {
                                Source = () => EcsCoordinator.GetBuildingComponent(world.SelectedEntity!.Value).BuildCost
                                    .WithRemove(ResourceMarker.All, EcsCoordinator.GetInventoryComponent(world.SelectedEntity!.Value).Inventory, ResourceMarker.Default, ResourceMarker.Default)
                                    .GetResourceQuantities(ResourceMarker.All),
                                ContainerView = new StackView(StackType.Vertical),
                                ItemView = rq => getResourceRowView(false, rq.Resource, () => rq.Quantity),
                                EmptyView = new LabelView
                                {
                                    Text = () => "No resources",
                                    FontSize = defaultFontSize,
                                    ForegroundColor = () => descriptionColor
                                }
                            },
                            getResourceRowView(true, null, () => EcsCoordinator.GetBuildingComponent(world.SelectedEntity!.Value).BuildWorkTicks),
                        }
                    }
                }
            }, Anchor.BottomLeft));

        // orders
        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                IsVisible = () => !world.SelectedEntity.HasValue,
                BackgroundColor = panelBackgroundColor,
                Padding = new(8),
                Children =
                {
                    new StackView(StackType.Vertical)
                    {
                        Children =
                        {
                            new LabelView() { Text = () => "Zones" },
                            new ButtonView()
                            {
                                Child = new LabelView() { Text = () => "Grow Zone" },
                                IsChecked = () => world.CurrentZoneType.HasValue,
                                Clicked = () => (world.CurrentZoneType, world.CurrentZoneStartPoint) =
                                    (ZoneType.Grow, null),
                            }
                        }
                    }
        //            new StackView(StackType.Vertical)
        //            {
        //                Visible = () => world.SelectedEntity is not null,
        //                Children =
        //                {
        //                    new LabelView
        //                    {
        //                        Text = () => world.SelectedEntity is Villager villager ? villager.AIPlan is { } aiPlan ? aiPlan.Description : "Idle."
        //                            : world.SelectedEntity is Building { IsBuilt: false} buildingSite ? $"This is a building site, waiting for {buildingSite.BuildCost} and {buildingSite.BuildWorkTicks} work ticks."
        //                            : $"This is a {(world.SelectedEntity switch { Building => "building", Tree => "cute tree", _ => "fluffy resource" })}, it's just existing.",
        //                        FontSize = defaultFontSize,
        //                        MinHeight = () => 35,
        //                        ForegroundColor = () => descriptionColor
        //                    },
        //                    new StackView(StackType.Horizontal)
        //                    {
        //                        Visible = () => world.SelectedEntity is Villager,
        //                        Children =
        //                        {
        //                            new LabelView
        //                            {
        //                                Text = () => "Needs:",
        //                                FontSize = defaultFontSize,
        //                                ForegroundColor = () => descriptionColor
        //                            },
        //                            // hunger block
        //                            new LabelView
        //                            {
        //                                Text = () => "Hunger",
        //                                ForegroundColor = () => descriptionColor,
        //                                FontSize = defaultFontSize,
        //                                Padding = new(20, 0, 0, 0)
        //                            },
        //                            new ProgressView
        //                            {
        //                                Value = () => ((Villager)world.SelectedEntity!).Needs.Hunger,
        //                                Maximum = () => ((Villager)world.SelectedEntity!).Needs.HungerMax,
        //                                StringFormat = () => "{0:0.0}%",
        //                                ForegroundColor = () => ((Villager)world.SelectedEntity!).Needs.Hunger / ((Villager)world.SelectedEntity!).Needs.HungerMax < ((Villager)world.SelectedEntity!).HungerThreshold
        //                                    ? Colors4.DarkGreen : Colors4.DarkRed,
        //                                TextColor = descriptionColor,
        //                                FontSize = defaultFontSize - 2,
        //                                MinWidth = () => 120
        //                            }
        //                        }
        //                    },
        //                    new StackView(StackType.Horizontal)
        //                    {
        //                        Visible = () => world.SelectedEntity is Building { IsBuilt: true, AssignedWorkers.Length: >0 },
        //                        Children =
        //                        {
        //                            new LabelView
        //                            {
        //                                Text = () => "Assigned workers: ",
        //                                FontSize = defaultFontSize,
        //                                ForegroundColor = () => descriptionColor
        //                            },
        //                            new RepeaterView<AssignedWorker>
        //                            {
        //                                Source = () => ((Building)world.SelectedEntity!).AssignedWorkers,
        //                                ContainerView = new StackView(StackType.Horizontal),
        //                                ItemView = aw => new StackView(StackType.Horizontal)
        //                                {
        //                                    Children =
        //                                    {
        //                                        new LabelView
        //                                        {
        //                                            Text = () => aw.Villager is { } assignedVillager ? assignedVillager.Name + (aw.VillagerWorking ? "" : "[SLK]") :"--",
        //                                            FontSize = defaultFontSize,
        //                                            ForegroundColor = () => highlightColor,
        //                                            Padding = new(10, 0, 0, 0)
        //                                        }
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    },
        //                    new LabelView
        //                    {
        //                        Text = () => "Inventory:",
        //                        FontSize = defaultFontSize,
        //                        ForegroundColor = () => descriptionColor
        //                    },
        //                    new RepeaterView<ResourceQuantity>
        //                    {
        //                        Padding = new(20, 0, 0, 0),
        //                        Source = () => world.SelectedEntity switch
        //                        {
        //                            Villager villager => villager.Inventory.ResourceQuantities.Where(rq => rq.Quantity > 0),
        //                            Building building => building.Inventory.ResourceQuantities.Where(rq => rq.Quantity > 0),
        //                            ResourceBucket resourceBucket => resourceBucket.ResourceQuantities.Where(rq => rq.Quantity > 0),
        //                            Tree tree => tree.Inventory.ResourceQuantities.Where(rq => rq.Quantity > 0),
        //                            _ => null
        //                        },
        //                        ContainerView = new StackView(StackType.Vertical),
        //                        ItemView = rq => new StackView(StackType.Horizontal)
        //                        {
        //                            Children =
        //                            {
        //                                new LabelView
        //                                {
        //                                    Text = () => rq.Quantity.ToString(),
        //                                    FontSize = defaultFontSize,
        //                                    MinWidth = () => 50,
        //                                    Margin = new(0,0,10,0),
        //                                    HorizontalTextAlignment = HorizontalAlignment.Right,
        //                                    ForegroundColor = () => highlightColor
        //                                },
        //                                new ImageView
        //                                {
        //                                    Source = () => GetImagePath(rq.Resource),
        //                                    InheritParentSize = true
        //                                },
        //                                new LabelView
        //                                {
        //                                    Text = () => $" {rq.Resource.Name}",
        //                                    FontSize = defaultFontSize,
        //                                    ForegroundColor = () => descriptionColor
        //                                }
        //                            }
        //                        },
        //                        EmptyView = new LabelView
        //                        {
        //                            Text = () => "Nothing",
        //                            FontSize = defaultFontSize,
        //                            ForegroundColor = () => descriptionColor
        //                        }
        //                    }
        //                }
        //            },
        //            new RepeaterView<string>
        //            {
        //                Visible = () => world.SelectedEntity is null,
        //                Source = () => world.BuildingTemplates,
        //                ContainerView = new StackView(StackType.Horizontal),
        //                ItemView = key => new ButtonView
        //                {
        //                    Clicked = () =>
        //                    {
        //                        world.CurrentBuildingTemplate = world.BuildingTemplates[key];
        //                        world.FireCurrentBuildingTemplateChanged();
        //                    },
        //                    Child = new StackView(StackType.Vertical)
        //                    {
        //                        Margin = new(0, 0, 20, 0),
        //                        Children =
        //                        {
        //                            new ImageView
        //                            {
        //                                Source = () => GetImagePath(world.BuildingTemplates[key]),
        //                                MinWidth = () => 0,
        //                            },
        //                            new LabelView
        //                            {
        //                                Text = () => world.BuildingTemplates[key].Name,
        //                                Margin = new(0, 5, 0, 0),
        //                                FontSize = defaultFontSize,
        //                                HorizontalTextAlignment = HorizontalAlignment.Center,
        //                            },
        //                        }
        //                    }
        //                }
        //            }
                }
            }, Anchor.BottomLeft));

        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                Children =
                {
                    new LabelView
                    {
                        Text = () => $"""
                            FPS: {Math.Round(FrameData.Rate, 0, MidpointRounding.ToPositiveInfinity):0}, Update: {FrameData.UpdateTimePercentage * 100:0.00}%, Render: {FrameData.RenderTimePercentage * 100:0.00}%
                            Draw calls: {FrameData.DrawCallCount}, Triangles: {FrameData.TriangleCount}, Lines: {FrameData.LineCount}
                            """,
                        FontSize = 22,
                        Padding = new(2),
                        ForegroundColor = () => Colors4.White,
                        BackgroundColor = new(0,0,0,.4f)
                    },
                    new LabelView
                    {
                        Text = () => "PAUSED",
                        IsVisible = () => world.TimeSpeedUp == 0,
                        FontSize = 22,
                        Padding = new(2, 0),
                        ForegroundColor = () => Colors4.Red,
                    },
                }
            }));
    }
}
