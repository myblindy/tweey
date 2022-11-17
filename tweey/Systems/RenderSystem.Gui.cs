namespace Tweey.Systems;

partial class RenderSystem
{
    Vector4 panelBackgroundColor = new(.2f, .2f, .2f, .5f);
    float defaultFontSize, largeFontSize, smallFontSize;

    void InitializeGui()
    {
        var descriptionColor = new Vector4(.8f, .8f, .8f, 1);
        var highlightColor = Colors4.Aqua;

        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                BackgroundColor = panelBackgroundColor,
                Children =
                {
                    new LabelView { Text = () => $"Time: {world.WorldTime}" },
                    new RepeaterView<double>
                    {
                        Source = () => new double[] { 0, 1, 2, 4, 8, 16 },
                        ContainerView = new StackView(StackType.Horizontal),
                        ItemView = s => new ButtonView
                        {
                            Child = new LabelView { Text = () => $"{s}x" },
                            IsChecked = () => world.TimeSpeedUp == s,
                            Clicked = () => world.TimeSpeedUp = s,
                        }
                    },
                }
            }, Anchor.BottomRight));

        static string? getEntityDescription(Entity entity) =>
            entity.HasVillagerComponent() ? "Villager"
            : entity.HasResourceComponent() ? "Resource"
            : entity.HasBuildingComponent() ? !entity.GetBuildingComponent().IsBuilt ? "Building Site" : "Building"
            : entity.HasPlantComponent() ? "Tree"
            : entity.HasZoneComponent() ? "Zone"
            : throw new NotImplementedException();

        static string? getEntityName(Entity entity) =>
            entity.HasIdentityComponent() ? entity.GetIdentityComponent().Name : entity.HasZoneComponent() ? entity.GetZoneComponent().Type.ToString() : null;

        View getResourceRowView(bool labor, Resource? resource, Func<double> quantity) =>
            new StackView(StackType.Horizontal)
            {
                Children =
                {
                    new LabelView
                    {
                        Text = () => quantity().ToString(),
                        FontSize = () => defaultFontSize,
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
                        FontSize = () => defaultFontSize,
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
                            new LabelView
                            {
                                Text = () => getEntityDescription(world.SelectedEntity!.Value),
                                FontSize = () => largeFontSize,
                                ForegroundColor = () => descriptionColor
                            },
                            new ImageView
                            {
                                Padding = new(10, 0, 0, 0),
                                Source = () => world.SelectedEntity!.Value.GetRenderableComponent().AtlasEntryName,
                                InheritParentSize = true,
                            },
                            new LabelView
                            {
                                Text = () => getEntityName(world.SelectedEntity!.Value),
                                FontSize = () => largeFontSize,
                                ForegroundColor = () => highlightColor
                            },
                        }
                    },

                    // building site details
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => world.SelectedEntity!.Value.HasBuildingComponent()
                            && !world.SelectedEntity!.Value.GetBuildingComponent().IsBuilt,
                        Children =
                        {
                            new LabelView
                            {
                                Padding = new(25, 15, 0, 0),
                                FontSize = () => defaultFontSize,
                                Text = () => "Required:"
                            },
                            new RepeaterView<ResourceQuantity>
                            {
                                Source = () => world.SelectedEntity!.Value.GetBuildingComponent().Template.BuildCost
                                    .WithRemove(ResourceMarker.All, world.SelectedEntity!.Value.GetInventoryComponent().Inventory, ResourceMarker.Default, ResourceMarker.Default)
                                    .GetResourceQuantities(ResourceMarker.All),
                                ContainerView = new StackView(StackType.Vertical),
                                ItemView = rq => getResourceRowView(false, rq.Resource, () => rq.Quantity),
                                EmptyView = new LabelView
                                {
                                    Text = () => "No resources",
                                    FontSize = () => defaultFontSize,
                                    ForegroundColor = () => descriptionColor
                                }
                            },
                            getResourceRowView(true, null, () => world.SelectedEntity!.Value.GetBuildingComponent().BuildWorkTicks),
                        }
                    },

                    // tree details
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => world.SelectedEntity!.Value.HasPlantComponent(),
                        Children =
                        {
                            new LabelView
                            {
                                Padding = new(25, 15, 0, 0),
                                FontSize = () => defaultFontSize,
                                Text = () => "Required:"
                            },
                            getResourceRowView(true, null, () => world.SelectedEntity!.Value.GetPlantComponent().WorkTicks),
                        }
                    },
                }
            }, Anchor.BottomLeft));

        // orders
        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Horizontal)
            {
                IsVisible = () => !world.SelectedEntity.HasValue,
                BackgroundColor = panelBackgroundColor,
                Children =
                {
                    new StackView(StackType.Vertical)
                    {
                        Children =
                        {
                            new LabelView { Text = () => "Zones:" },
                            new StackView(StackType.Horizontal)
                            {
                                Children =
                                {
                                    new ButtonView
                                    {
                                        Child = new LabelView { Text = () => "Grow Zone" },
                                        IsChecked = () => world.CurrentWorldTemplate.ZoneType == ZoneType.Grow,
                                        Clicked = () => (world.CurrentWorldTemplate.ZoneType, world.CurrentZoneStartPoint) =
                                            (ZoneType.Grow, null),
                                    },
                                    new ButtonView
                                    {
                                        Child = new LabelView { Text = () => "Harvest" },
                                        IsChecked = () => world.CurrentWorldTemplate.ZoneType == ZoneType.MarkHarvest,
                                        Clicked = () => (world.CurrentWorldTemplate.ZoneType, world.CurrentZoneStartPoint) =
                                            (ZoneType.MarkHarvest, null),
                                    },
                                }
                            }
                        }
                    },
                    new StackView(StackType.Vertical)
                    {
                        Children =
                        {
                            new LabelView { Text = () => "Buildings:" },
                            new RepeaterView<BuildingTemplate>
                            {
                                Source = () => world.BuildingTemplates,
                                ContainerView = new StackView(StackType.Horizontal),
                                ItemView = bt => new ButtonView
                                {
                                    Child = new LabelView { Text = () => bt.Name },
                                    IsChecked = () => world.CurrentWorldTemplate.BuildingTemplate == bt,
                                    Clicked = () => world.CurrentWorldTemplate.BuildingTemplate= bt,
                                }
                            }
                        }
                    },
                }
            }, Anchor.BottomLeft));

        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                Children =
                {
                    new LabelView
                    {
                        Text = () => $$"""
                            FPS: {{Math.Round(FrameData.Rate, 0, MidpointRounding.ToPositiveInfinity):0}}, Update: {{FrameData.UpdateTimePercentage * 100:0.00}}%, Render: {{FrameData.RenderTimePercentage * 100:0.00}}%
                            Draw calls: {{FrameData.DrawCallCount}}, Triangles: {{FrameData.TriangleCount}}, Lines: {{FrameData.LineCount}}
                            {{string.Join(Environment.NewLine, EcsCoordinator.SystemTimingInformation.Select((kvp, idx)=> $"{kvp.Key}: {FrameData.GetCustomTimePercentage(idx):0.00}%"))}}
                            SwapBuffer: {{FrameData.GetCustomTimePercentage(EcsCoordinator.SystemsCount):0.00}}%
                            """,
                        FontSize = () => smallFontSize,
                        Padding = new(2),
                        ForegroundColor = () => Colors4.White,
                        BackgroundColor = new(0,0,0,.4f)
                    },
                    new LabelView
                    {
                        Text = () => "PAUSED",
                        IsVisible = () => world.TimeSpeedUp == 0,
                        FontSize = () => smallFontSize,
                        Padding = new(2, 0),
                        ForegroundColor = () => Colors4.Red,
                    },
                }
            }));
    }
}
