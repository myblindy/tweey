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
            entity.HasVillagerComponent() ? "Villager"
            : entity.HasResourceComponent() ? "Resource"
            : entity.HasBuildingComponent() ? !entity.GetBuildingComponent().IsBuilt ? "Building Site" : "Building"
            : entity.HasTreeComponent() ? "Tree"
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
                            new LabelView
                            {
                                Text = () => getEntityDescription(world.SelectedEntity!.Value),
                                FontSize = 30,
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
                                FontSize = 30,
                                ForegroundColor = () => highlightColor
                            },
                        }
                    },

                    // building site details
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => world.SelectedEntity!.Value.HasBuildingComponent()
                            &&!world.SelectedEntity!.Value.GetBuildingComponent().IsBuilt,
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
                                Source = () => world.SelectedEntity!.Value.GetBuildingComponent().Template.BuildCost
                                    .WithRemove(ResourceMarker.All, world.SelectedEntity!.Value.GetInventoryComponent().Inventory, ResourceMarker.Default, ResourceMarker.Default)
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
                            getResourceRowView(true, null, () => world.SelectedEntity!.Value.GetBuildingComponent().BuildWorkTicks),
                        }
                    }
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
                            new ButtonView
                            {
                                Child = new LabelView { Text = () => "Grow Zone" },
                                IsChecked = () => world.CurrentZoneType.HasValue,
                                Clicked = () => (world.CurrentZoneType, world.CurrentZoneStartPoint, world.CurrentBuildingTemplate) =
                                    (ZoneType.Grow, null, null),
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
                                    IsChecked = () => world.CurrentBuildingTemplate == bt,
                                    Clicked = () => (world.CurrentZoneType, world.CurrentBuildingTemplate) = (default, bt),
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
                        FontSize = 18,
                        Padding = new(2),
                        ForegroundColor = () => Colors4.White,
                        BackgroundColor = new(0,0,0,.4f)
                    },
                    new LabelView
                    {
                        Text = () => "PAUSED",
                        IsVisible = () => world.TimeSpeedUp == 0,
                        FontSize = 18,
                        Padding = new(2, 0),
                        ForegroundColor = () => Colors4.Red,
                    },
                }
            }));
    }
}
