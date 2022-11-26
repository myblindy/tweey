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
            : entity.HasPlantComponent() ? "Plant"
            : entity.HasZoneComponent() ? "Zone"
            : throw new NotImplementedException();

        string? getEntityImage(Entity entity) =>
            entity.HasPlantComponent() && entity.GetPlantComponent() is { } plantComponent ? plantComponent.Template.GetImageFileName(plantComponent.GetGrowth(world))
            : entity.GetRenderableComponent().AtlasEntryName;

        static string? getEntityName(Entity entity) =>
            entity.HasIdentityComponent() ? entity.GetIdentityComponent().Name : entity.HasZoneComponent() ? entity.GetZoneComponent().Type.ToString() : null;

        View getResourceRowView(bool labor, Resource? resource, Func<double> quantity, Func<ResourceMarker>? marker = null) =>
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
                        Text = () => $" {(labor ? "Work" : resource!.Name)}{(marker is null || marker() == ResourceMarker.Default ? null : $" [{marker()}]")}",
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
                                Source = () => getEntityImage(world.SelectedEntity!.Value),
                                InheritParentSize = true,
                            },
                            new LabelView
                            {
                                Text = () => getEntityName(world.SelectedEntity!.Value),
                                FontSize = () => largeFontSize,
                                ForegroundColor = () => highlightColor
                            },
                            new LabelView
                            {
                                Text = () => $" ID {world.SelectedEntity!.Value}",
                                FontSize = () => smallFontSize,
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

                    // plant details
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => world.SelectedEntity!.Value.HasPlantComponent(),
                        Children =
                        {
                            new LabelView
                            {
                                Padding = new(25, 15, 0, 0),
                                FontSize = () => defaultFontSize,
                                Text = () => $"Growth: {world.SelectedEntity!.Value.GetPlantComponent().GetGrowth(world) * 100:0}%"
                            },
                            new LabelView
                            {
                                Padding = new(25, 0, 0, 0),
                                FontSize = () => defaultFontSize,
                                Text = () => "Required for harvesting:"
                            },
                            getResourceRowView(true, null, () => world.SelectedEntity!.Value.GetPlantComponent().WorkTicks),
                        }
                    },

                    // resource details
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => world.SelectedEntity!.Value.HasResourceComponent(),
                        Children =
                        {
                            new LabelView
                            {
                                Padding = new(25, 15, 0, 0),
                                FontSize = () => defaultFontSize,
                                Text = () => "Inventory:"
                            },
                            new RepeaterView<(ResourceQuantity rq, ResourceMarker marker)>
                            {
                                Source = () => world.SelectedEntity!.Value.GetInventoryComponent().Inventory.GetResourceQuantitiesWithMarkers(ResourceMarker.All),
                                ContainerView = new StackView(StackType.Vertical),
                                ItemView = rqm => getResourceRowView(false, rqm.rq.Resource, () => rqm.rq.Quantity, () => rqm.marker),
                                EmptyView = new LabelView
                                {
                                    Text = () => "Nothing",
                                    FontSize = () => defaultFontSize,
                                    ForegroundColor = () => descriptionColor
                                }
                            },
                        }
                    },

                    // bills
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => world.SelectedEntity.HasValue && world.SelectedEntity.Value.HasWorkableComponent()
                            && world.SelectedEntity.Value.HasBuildingComponent()
                            && world.SelectedEntity.Value.GetBuildingComponent().IsBuilt
                            && world.SelectedEntity.Value.GetBuildingComponent().Template.ProductionLines.Count > 0,
                        Children =
                        {
                            new StackView(StackType.Horizontal)
                            {
                                Children =
                                {
                                    new LabelView
                                    {
                                        Text = () => "Bills: "
                                    },
                                    new ButtonView
                                    {
                                        Clicked = () => world.SelectedEntity!.Value.GetWorkableComponent().Bills.Add(new()
                                        {
                                            ProductionLine = world.SelectedEntity.Value.GetBuildingComponent().Template.ProductionLines[0],
                                            AmountType = BillAmountType.FixedValue,
                                            Amount = 10,
                                        }),
                                        Child = new LabelView { Text = () => "Add" }
                                    }
                                }
                            },
                            new RepeaterView<Bill>
                            {
                                Source = () => world.SelectedEntity!.Value.GetWorkableComponent().Bills,
                                ItemView = b => new StackView(StackType.Horizontal)
                                {
                                    Children =
                                    {
                                        new ButtonView
                                        {
                                            Child = new LabelView
                                            {
                                                Text = () => "Del",
                                            }
                                        },
                                        new ButtonView
                                        {
                                            Child = new LabelView
                                            {
                                                Text = () => b.ProductionLine.Name,
                                            }
                                        },
                                        new ButtonView
                                        {
                                            Clicked = () => b.AmountType = b.AmountType switch
                                            {
                                                BillAmountType.FixedValue => BillAmountType.UntilInStock,
                                                BillAmountType.UntilInStock => BillAmountType.FixedValue,
                                                _ => throw new NotImplementedException()
                                            },
                                            Child = new LabelView
                                            {
                                                Text = () => b.AmountType switch
                                                {
                                                    BillAmountType.UntilInStock => $" until you have {b.Amount} in stock.",
                                                    BillAmountType.FixedValue => $" {b.Amount} times.",
                                                    _ => throw new NotImplementedException()
                                                }
                                            }
                                        },
                                        new ButtonView
                                        {
                                            Clicked = () => --b.Amount,
                                            Child = new LabelView
                                            {
                                                Text = () => "-",
                                            }
                                        },
                                        new ButtonView
                                        {
                                            Clicked = () => ++b.Amount,
                                            Child = new LabelView
                                            {
                                                Text = () => "+",
                                            }
                                        },
                                    }
                                }
                            }
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
                            new LabelView { Text = () => "Orders:" },
                            new StackView(StackType.Horizontal)
                            {
                                Children =
                                {
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
                            new LabelView { Text = () => "Zones:" },
                            new StackView(StackType.Horizontal)
                            {
                                Children =
                                {
                                    new ButtonView
                                    {
                                        Child = new LabelView { Text = () => "Grow" },
                                        IsChecked = () => world.CurrentWorldTemplate.ZoneType == ZoneType.Grow,
                                        Clicked = () => (world.CurrentWorldTemplate.ZoneType, world.CurrentZoneStartPoint) =
                                            (ZoneType.Grow, null),
                                    },
                                    new ButtonView
                                    {
                                        Child = new LabelView { Text = () => "Storage" },
                                        IsChecked = () => world.CurrentWorldTemplate.ZoneType == ZoneType.Storage,
                                        Clicked = () => (world.CurrentWorldTemplate.ZoneType, world.CurrentZoneStartPoint) =
                                            (ZoneType.Storage, null),
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
                            {{string.Join(Environment.NewLine, EcsCoordinator.SystemTimingInformation.Select((kvp, idx)=> $"{kvp.Key}: {FrameData.GetCustomTimePercentage(idx) * 100:0.00}%"))}}
                            SwapBuffer: {{FrameData.GetCustomTimePercentage(EcsCoordinator.SystemsCount) * 100:0.00}}%
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
