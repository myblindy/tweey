namespace Tweey.Renderer;

partial class WorldRenderer
{
    void InitializeGui()
    {
        var descriptionColor = new Vector4(.8f, .8f, .8f, 1);
        var highlightColor = Colors4.Aqua;
        var defaultFontSize = 18;

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
                                            : world.SelectedEntity is ResourceBucket ? "Resource "
                                            : world.SelectedEntity is Villager ? "Villager "
                                            : world.SelectedEntity is Tree ? "Tree "
                                            : throw new InvalidOperationException(),
                                        FontSize = 30,
                                        ForegroundColor = () => descriptionColor
                                    },
                                    new LabelView
                                    {
                                        Text = () => world.SelectedEntity!.Name,
                                        FontSize = 30,
                                        ForegroundColor = () => highlightColor
                                    },
                                }
                            },
                            new LabelView
                            {
                                Text = () => world.SelectedEntity is Villager villager ? villager.AIPlan is { } aiPlan ? aiPlan.Description : "Idle."
                                    : world.SelectedEntity is Building { IsBuilt: false} buildingSite ? $"This is a building site, waiting for {buildingSite.BuildCost} and {buildingSite.BuildWorkTicks} work ticks."
                                    : $"This is a {(world.SelectedEntity switch { Building => "building", Tree => "cute tree", _ => "fluffy resource" })}, it's just existing.",
                                FontSize = defaultFontSize,
                                MinHeight = () => 35,
                                ForegroundColor = () => descriptionColor
                            },
                            new LabelView
                            {
                                Text = () => "Needs:",
                                FontSize = defaultFontSize,
                                ForegroundColor = () => descriptionColor
                            },
                            new StackView(StackType.Horizontal)
                            {
                                Visible = () => world.SelectedEntity is Villager,
                                Children =
                                {
                                    // hunger block
                                    new LabelView
                                    {
                                        Text = () => "Hunger",
                                        ForegroundColor = () => descriptionColor,
                                        FontSize = defaultFontSize,
                                        Padding = new(20, 0, 0, 0)
                                    },
                                    new ProgressView
                                    {
                                        Value = () => ((Villager)world.SelectedEntity!).Needs.Hunger,
                                        Maximum = () => ((Villager)world.SelectedEntity!).Needs.HungerMax,
                                        StringFormat = () => "{0:0.0}%",
                                        ForegroundColor = () => ((Villager)world.SelectedEntity!).Needs.Hunger / ((Villager)world.SelectedEntity!).Needs.HungerMax < ((Villager)world.SelectedEntity!).HungerThreshold
                                            ? Colors4.DarkGreen : Colors4.DarkRed,
                                        TextColor = descriptionColor,
                                        FontSize = defaultFontSize - 2,
                                        MinWidth = () => 120
                                    }
                                }
                            },
                            new LabelView
                            {
                                Text = () => "Inventory:",
                                FontSize = defaultFontSize,
                                ForegroundColor = () => descriptionColor
                            },
                            new RepeaterView<ResourceQuantity>
                            {
                                Padding = new(20, 0, 0, 0),
                                Source = () => world.SelectedEntity switch
                                {
                                    Villager villager => villager.Inventory.ResourceQuantities.Where(rq => rq.Quantity > 0),
                                    Building building => building.Inventory.ResourceQuantities.Where(rq => rq.Quantity > 0),
                                    ResourceBucket resourceBucket => resourceBucket.ResourceQuantities.Where(rq => rq.Quantity > 0),
                                    Tree tree => tree.Inventory.ResourceQuantities.Where(rq => rq.Quantity > 0),
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
                                            FontSize = defaultFontSize,
                                            MinWidth = () => 50,
                                            Margin = new(0,0,10,0),
                                            HorizontalTextAlignment = HorizontalAlignment.Right,
                                            ForegroundColor = () => highlightColor
                                        },
                                        new ImageView
                                        {
                                            Source = () => GetImagePath(rq.Resource),
                                            InheritParentSize = true
                                        },
                                        new LabelView
                                        {
                                            Text = () => $" {rq.Resource.Name}",
                                            FontSize = defaultFontSize,
                                            ForegroundColor = () => descriptionColor
                                        }
                                    }
                                },
                                EmptyView = new LabelView
                                {
                                    Text = () => "Nothing",
                                    FontSize = defaultFontSize,
                                    ForegroundColor = () => descriptionColor
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
                                Margin = new(0, 0, 20, 0),
                                Children =
                                {
                                    new ImageView
                                    {
                                        Source = () => GetImagePath(world.BuildingTemplates[key]),
                                        MinWidth = () => 0,
                                    },
                                    new LabelView
                                    {
                                        Text = () => world.BuildingTemplates[key].Name,
                                        Margin = new(0, 5, 0, 0),
                                        FontSize = defaultFontSize,
                                        HorizontalTextAlignment = HorizontalAlignment.Center
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
                        Visible = () => world.Paused,
                        FontSize = 22,
                        Padding = new(2, 0),
                        ForegroundColor = () => Colors4.Red,
                    },
                }
            }));
    }
}
