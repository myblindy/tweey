namespace Tweey.Renderer;

partial class WorldRenderer
{
    void InitializeGui()
    {
        var descriptionColor = new Vector4(.8f, .8f, .8f, 1);
        var highlightColor = Colors.Aqua;
        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                BackgroundColor = new(.1f, .1f, .1f, 1),
                MinWidth = () => WidthPercentage(50),
                MinHeight = () => HeightPercentage(20),
                Padding = new(8),
                Children =
                {
                    new StackView(StackType.Vertical)
                    {
                        Visible = () => world.SelectedEntity is not null,
                        Children =
                        {
                            new StackView(StackType.Horizontal)
                            {
                                Children =
                                {
                                    new ImageView
                                    {
                                        Source = () => GetImagePath(world.SelectedEntity!),
                                        InheritParentSize = true,
                                    },
                                    new LabelView
                                    {
                                        Text = () => world.SelectedEntity is Building building ? building.IsBuilt ? "Building " : "Building Site "
                                            : world.SelectedEntity is ResourceBucket ? "Resources"
                                            : world.SelectedEntity is Villager ? "Villager"
                                            : throw new InvalidOperationException(),
                                        FontSize = 30,
                                        ForegroundColor = descriptionColor
                                    },
                                    new LabelView
                                    {
                                        Text = () => world.SelectedEntity!.Name,
                                        FontSize = 30,
                                        ForegroundColor = highlightColor
                                    },
                                }
                            },
                            new LabelView
                            {
                                Text = () => world.SelectedEntity is Villager villager ? villager.AIPlan is { } aiPlan ? aiPlan.Description : "Idle."
                                    : world.SelectedEntity is Building { IsBuilt: false} buildingSite ? $"This is a building site, waiting for {buildingSite.BuildCost} and {buildingSite.BuildWorkTicks} work ticks."
                                    : $"This is a {(world.SelectedEntity is Building ? "building" : "resource")}, it's just existing.",
                                FontSize = 18,
                                MinHeight = () => 35,
                                ForegroundColor = descriptionColor
                            },
                            new LabelView
                            {
                                Text = () => "Inventory:",
                                FontSize = 18,
                                ForegroundColor = descriptionColor
                            },
                            new RepeaterView<ResourceQuantity>
                            {
                                Source = () => world.SelectedEntity switch
                                {
                                    Villager villager => villager.Inventory.ResourceQuantities.Where(rq => rq.Quantity > 0),
                                    Building building => building.Inventory.ResourceQuantities.Where(rq => rq.Quantity > 0),
                                    ResourceBucket resourceBucket => resourceBucket.ResourceQuantities.Where(rq => rq.Quantity > 0),
                                    _ => null
                                },
                                ContainerView = new StackView(StackType.Vertical),
                                ItemView = rq => new StackView(StackType.Horizontal)
                                {
                                    Children =
                                    {
                                        new LabelView
                                        {
                                            Text = () => rq.Quantity.ToString(),
                                            FontSize = 18,
                                            MinWidth = () => 50,
                                            Margin = new(0,0,10,0),
                                            HorizontalTextAlignment = HorizontalAlignment.Right,
                                            ForegroundColor = highlightColor
                                        },
                                        new ImageView
                                        {
                                            Source = () => GetImagePath(rq.Resource),
                                            InheritParentSize = true
                                        },
                                        new LabelView
                                        {
                                            Text = () => $" {rq.Resource.Name}",
                                            FontSize = 18,
                                            ForegroundColor = descriptionColor
                                        }
                                    }
                                },
                                EmptyView = new LabelView
                                {
                                    Text = () => "Nothing",
                                    FontSize = 18,
                                    ForegroundColor = descriptionColor
                                }
                            }
                        }
                    },
                    new RepeaterView<string>
                    {
                        Visible = () => world.SelectedEntity is null,
                        Source = () => world.BuildingTemplates,
                        ContainerView = new StackView(StackType.Horizontal),
                        ItemView = key => new ButtonView
                        {
                            Clicked = () =>
                            {
                                world.CurrentBuildingTemplate = world.BuildingTemplates[key];
                                world.FireCurrentBuildingTemplateChanged();
                            },
                            Child = new StackView(StackType.Vertical)
                            {
                                Children =
                                {
                                    new ImageView
                                    {
                                        Source = () => GetImagePath(world.BuildingTemplates[key]),
                                        InheritParentSize = true,
                                    },
                                    new LabelView
                                    {
                                        Text = () => world.BuildingTemplates[key].Name,
                                    },
                                }
                            }
                        }
                    }
                }
            }, Anchor.BottomLeft));

        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                Children =
                {
                    new LabelView
                    {
                        Text = () => $"FPS: {Math.Round(frameData.Rate, 1, MidpointRounding.ToPositiveInfinity):0.0}, update: {frameData.UpdateTimePercentage * 100:0.00}%, render: {frameData.RenderTimePercentage * 100:0.00}%",
                        FontSize = 22,
                        Padding = new(2),
                        ForegroundColor = Colors.Lime
                    },
                    new LabelView
                    {
                        Text = () => "PAUSED",
                        Visible = () => world.Paused,
                        FontSize = 22,
                        Padding = new(2, 0),
                        ForegroundColor = Colors.Red,
                    },
                }
            }));
    }
}
