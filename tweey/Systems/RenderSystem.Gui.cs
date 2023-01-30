using Tweey.Support.AI.SystemJobs;

namespace Tweey.Systems;

partial class RenderSystem
{
    Vector4 panelBackgroundColor = new(.2f, .2f, .2f, .5f);
    float defaultFontSize, largeFontSize, smallFontSize;

    void InitializeGui()
    {
        var descriptionColor = new Vector4(.8f, .8f, .8f, 1);
        var highlightColor = Colors4.Aqua;

        // time gui
        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                BackgroundColor = panelBackgroundColor,
                Children =
                {
                    new LabelView(() => $"Time: {world.WorldTime}") { FontSize = () => smallFontSize },
                    new RepeaterView<double>
                    {
                        Source = () => new double[] { 0, 1, 2, 4, 8, 16 },
                        ContainerView = new StackView(StackType.Horizontal),
                        ItemView = (s, _) => new ButtonView(() => $"{s}x", () => smallFontSize)
                        {
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
            : null;

        string? getEntityImage(Entity entity) =>
            entity.HasPlantComponent() && entity.GetPlantComponent() is { } plantComponent ? plantComponent.Template.GetImageFileName(plantComponent.GetGrowth(world))
            : entity.GetRenderableComponent().AtlasEntryName;

        static string? getEntityName(Entity entity) =>
            entity.HasIdentityComponent() ? entity.GetIdentityComponent().Name : entity.HasZoneComponent() ? entity.GetZoneComponent().Type.ToString() : null;

        View getNeedsView(string name, Func<double> valueGen, Func<double> valueMaxGen) =>
            new StackView(StackType.Horizontal)
            {
                Children =
                {
                    new LabelView($"{name}: ")
                    {
                        FontSize = () => smallFontSize,
                        MinWidth = () => (int)WidthPercentage(3.2f)
                    },
                    new ProgressView
                    {
                        Maximum = valueMaxGen,
                        Value = valueGen,
                        StringFormat = () => "{0:0}%",
                        FontSize = () => smallFontSize,
                        MinWidth = () => (int)WidthPercentage(6),
                        ForegroundColor = () => valueGen() / valueMaxGen() > 0.2 ? Colors4.DarkGreen : Colors4.DarkRed
                    }
                }
            };

        View getResourceRowView(bool labor, Resource? resource, Func<double> quantity, Func<ResourceMarker>? marker = null) =>
            new StackView(StackType.Horizontal)
            {
                Children =
                {
                    new LabelView(() => quantity().ToString())
                    {
                        MinWidth = () => 50,
                        Margin = () => new(10, 0),
                        HorizontalTextAlignment = HorizontalAlignment.Right,
                        ForegroundColor = () => highlightColor
                    },
                    new ImageView
                    {
                        Source = () => labor ? "Data/Resources/labor.png" : resource!.ImageFileName,
                        InheritParentSize = true
                    },
                    new LabelView(() => $" {(labor ? "Work" : resource!.Name)}{(marker is null || marker() == ResourceMarker.Unmarked ? null : $" [{marker()}]")}")
                    {
                        ForegroundColor = () => descriptionColor
                    }
                }
            };

        PooledCollection<Entity> getVillagers()
        {
            var result = CollectionPool<Entity>.Get();
            EcsCoordinator.IterateVillagerArchetype((in EcsCoordinator.VillagerIterationResult vw) => result.Add(vw.Entity));

            return result;
        }

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
                            new LabelView(() => getEntityDescription(world.SelectedEntity!.Value))
                            {
                                FontSize = () => largeFontSize,
                                ForegroundColor = () => descriptionColor
                            },
                            new ImageView
                            {
                                Padding = new(10, 0, 0, 0),
                                Source = () => getEntityImage(world.SelectedEntity!.Value),
                                InheritParentSize = true,
                            },
                            new LabelView(() => getEntityName(world.SelectedEntity!.Value))
                            {
                                FontSize = () => largeFontSize,
                                ForegroundColor = () => highlightColor
                            },
                            new LabelView(() => $" ID {world.SelectedEntity!.Value}")
                            {
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
                            new LabelView("Required:")
                            {
                                Padding = new(25, 15, 0, 0),
                            },
                            new RepeaterView<ResourceQuantity>
                            {
                                Source = () => world.SelectedEntity!.Value.GetBuildingComponent().Template.BuildCost
                                    .WithRemove(ResourceMarker.All, world.SelectedEntity!.Value.GetInventoryComponent().Inventory, ResourceMarker.Unmarked, ResourceMarker.Unmarked)
                                    .GetResourceQuantities(ResourceMarker.All),
                                ContainerView = new StackView(StackType.Vertical),
                                ItemView = (rq, _) => getResourceRowView(false, rq.Resource, () => rq.Quantity),
                                EmptyView = new LabelView("No resources")
                                {
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
                            new LabelView("Inventory:")
                            {
                                Padding = new(25, 15, 0, 0),
                            },
                            new RepeaterView<(ResourceQuantity rq, ResourceMarker marker)>
                            {
                                Source = () => world.SelectedEntity!.Value.GetInventoryComponent().Inventory.GetResourceQuantitiesWithMarkers(ResourceMarker.All),
                                ContainerView = new StackView(StackType.Vertical),
                                ItemView = (rqm, _) => getResourceRowView(false, rqm.rq.Resource,() => rqm.rq.Quantity,() => rqm.marker),
                                EmptyView = new LabelView("Nothing")
                                {
                                    ForegroundColor = () => descriptionColor
                                }
                            },
                        }
                    },

                    // mood & thoughts
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => world.SelectedEntity.HasValue && world.SelectedEntity.Value.HasVillagerComponent(),
                        Children =
                        {
                            new StackView(StackType.Horizontal)
                            {
                                Children =
                                {
                                    new LabelView("Mood: ")
                                    {
                                        ForegroundColor = () => descriptionColor
                                    },
                                    new ProgressView
                                    {
                                        Maximum = () => 100,
                                        Value = () => world.SelectedEntity!.Value.GetVillagerComponent().MoodPercentage,
                                        StringFormat = () => "{0:0}%",
                                        FontSize = () => smallFontSize,
                                        MinWidth = () => (int)WidthPercentage(6),
                                        ForegroundColor = () => world.SelectedEntity!.Value.GetVillagerComponent().MoodPercentage / 100 > 0.2
                                            ? Colors4.DarkGreen : Colors4.DarkRed
                                    },
                                    new LabelView(() => world.SelectedEntity!.Value.GetVillagerComponent().MoodPercentageTarget == world.SelectedEntity!.Value.GetVillagerComponent().MoodPercentage ? ""
                                        : $" (target: {world.SelectedEntity!.Value.GetVillagerComponent().MoodPercentageTarget}%)", () => smallFontSize)
                                    {
                                        ForegroundColor = () => descriptionColor
                                    }
                                }
                            },
                            new LabelView("Thoughts:"),
                            new RepeaterView<(ThoughtTemplate thought, int stacks)>
                            {
                                Source = () => world.SelectedEntity!.Value.GetVillagerComponent().Thoughts.GroupBy(w => w.Template).Select(w => (w.Key, w.Count())),
                                ContainerView = new StackView(StackType.Vertical),
                                ItemView = (w, _) => new StackView(StackType.Horizontal)
                                {
                                    Children =
                                    {
                                        new LabelView($"{w.thought.Description}{(w.stacks>1 ? $" x{w.stacks}" : null)} ", () => smallFontSize)
                                        {
                                            ForegroundColor = () => descriptionColor
                                        },
                                        new LabelView(w.thought.MoodChange >= 0 ? $"+{w.thought.MoodChange}" : $"{w.thought.MoodChange}", () => smallFontSize)
                                        {
                                            ForegroundColor = () => highlightColor
                                        }
                                    }
                                }
                            }
                        }
                    },

                    // needs
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => world.SelectedEntity.HasValue && world.SelectedEntity.Value.HasVillagerComponent(),
                        Children =
                        {
                            new LabelView("Needs:"),
                            new StackView(StackType.Vertical)
                            {
                                Children =
                                {
                                    getNeedsView("Tired", () => world.SelectedEntity!.Value.GetVillagerComponent().Needs.Tired,
                                        () => world.SelectedEntity!.Value.GetVillagerComponent().Needs.TiredMax),
                                    getNeedsView("Poop", () => world.SelectedEntity!.Value.GetVillagerComponent().Needs.Poop,
                                        () => world.SelectedEntity!.Value.GetVillagerComponent().Needs.PoopMax),
                                    getNeedsView("Hunger", () => world.SelectedEntity!.Value.GetVillagerComponent().Needs.Hunger,
                                        () => world.SelectedEntity!.Value.GetVillagerComponent().Needs.HungerMax),
                                }
                            }
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
                                    new LabelView("Bills: "),
                                    new ButtonView("Add")
                                    {
                                        Clicked = () => world.SelectedEntity!.Value.GetWorkableComponent().Bills.Add(new()
                                        {
                                            ProductionLine = world.SelectedEntity.Value.GetBuildingComponent().Template.ProductionLines[0],
                                            AmountType = BillAmountType.FixedValue,
                                            Amount = 10,
                                        })
                                    }
                                }
                            },
                            new RepeaterView<Bill>
                            {
                                Source = () => world.SelectedEntity!.Value.GetWorkableComponent().Bills,
                                ItemView = (b, _) => new StackView(StackType.Horizontal)
                                {
                                    Children =
                                    {
                                        new ButtonView("Del"),
                                        new ButtonView(() => b.ProductionLine.Name),
                                        new ButtonView(() => b.AmountType switch
                                            {
                                                BillAmountType.FixedValue => $" {b.Amount} times.",
                                                BillAmountType.UntilInStock => $" until you have {b.Amount} in stock.",
                                                _ => throw new NotImplementedException()
                                            })
                                        {
                                            Clicked = async () =>
                                            {
                                                var (_, idx) = await CreatePickerClicked("Bill Type", new[]{ "Fixed Value", "Until in Stock" });
                                                if(idx >= 0)
                                                    b.AmountType = (BillAmountType)idx;
                                            }
                                        },
                                        new ButtonView("-")
                                        {
                                            Clicked = () => --b.Amount,
                                        },
                                        new ButtonView("+")
                                        {
                                            Clicked = () => ++b.Amount,
                                        },
                                    }
                                }
                            }
                        }
                    },
                }
            }, Anchor.BottomLeft));

        // orders, priorities
        var bottomTabs = new[] { "Orders", "Priorities" };
        var selectedBottomTab = 0;
        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                IsVisible = () => !world.SelectedEntity.HasValue,
                BackgroundColor = panelBackgroundColor,
                Children =
                {
                    // orders
                    new StackView(StackType.Vertical)
                    {
                        IsVisible = () => selectedBottomTab == 0,
                        Children =
                        {
                            new StackView(StackType.Vertical)
                            {
                                Children =
                                {
                                    new LabelView("Orders:"),
                                    new StackView(StackType.Horizontal)
                                    {
                                        Children =
                                        {
                                            new ButtonView("Harvest")
                                            {
                                                IsChecked = () => world.CurrentWorldTemplate.ZoneType == ZoneType.MarkHarvest,
                                                Clicked = () => (world.CurrentWorldTemplate.ZoneType, world.CurrentTemplateStartPoint) =
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
                                    new LabelView("Zones:"),
                                    new StackView(StackType.Horizontal)
                                    {
                                        Children =
                                        {
                                            new ButtonView("Grow")
                                            {
                                                IsChecked = () => world.CurrentWorldTemplate.ZoneType == ZoneType.Grow,
                                                Clicked = () => (world.CurrentWorldTemplate.ZoneType, world.CurrentTemplateStartPoint) =
                                                    (ZoneType.Grow, null),
                                            },
                                            new ButtonView("Storage")
                                            {
                                                IsChecked = () => world.CurrentWorldTemplate.ZoneType == ZoneType.Storage,
                                                Clicked = () => (world.CurrentWorldTemplate.ZoneType, world.CurrentTemplateStartPoint) =
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
                                    new LabelView("Buildings:"),
                                    new RepeaterView<BuildingTemplate>
                                    {
                                        Source = () => world.BuildingTemplates,
                                        ContainerView = new StackView(StackType.Horizontal),
                                        ItemView = (bt, _) => new ButtonView(() => bt.Name)
                                        {
                                            IsChecked = () => world.CurrentWorldTemplate.BuildingTemplate == bt,
                                            Clicked = () => (world.CurrentWorldTemplate.BuildingTemplate, world.CurrentTemplateStartPoint) =
                                                (bt, null),
                                        }
                                    }
                                }
                            },

                        }
                    },

                    // priorities
                    new StackView(StackType.Horizontal)
                    {
                        IsVisible = () => selectedBottomTab == 1,
                        Children =
                        {
                            new RepeaterView<(int index, BaseSystemJob? job)>
                            {
                                Source = () => EcsCoordinator.AISystem!.SystemJobs.Select((j, idx) => (idx, job: (BaseSystemJob?)j)).Where(w => w.job!.IsConfigurable).Prepend((-1, null)),
                                ContainerView = new StackView(StackType.Horizontal),
                                ItemView = (w, _) => w.job is null
                                    ? new RepeaterView<Entity>
                                    {
                                        ContainerView = new StackView(StackType.Vertical),
                                        Source = () => getVillagers().Prepend(Entity.Invalid),
                                        ItemView = (villager, villagerIdx) => new LabelView(villager == Entity.Invalid ? static () => null : () => $"{villager.GetIdentityComponent().Name}    ")
                                        {
                                            Margin = () => villagerIdx == 0 ? new(0, 0, 0, (int)HeightPercentage(3.05f)) : new(0, 0, 0, (int)HeightPercentage(2.4f))
                                        }
                                    }
                                    : new RepeaterView<Entity>
                                    {
                                        Source = () => getVillagers().Prepend(Entity.Invalid),
                                        ItemView = (villager, _) => villager == Entity.Invalid
                                            ? new LabelView(() => $"{w.job.Name}    ")
                                            : new ButtonView(() => (villager.GetWorkerComponent().SystemJobPriorities[w.index] + 1).ToString())
                                            {
                                                Clicked = () => villager.GetWorkerComponent().SystemJobPriorities[w.index] = (villager.GetWorkerComponent().SystemJobPriorities[w.index] + 1) % 5
                                            }
                                    }
                            }
                        }
                    },

                    // selector
                    new RepeaterView<string>
                    {
                        Margin = () => new(0, (int)HeightPercentage(10), 0, 0),
                        Source = () => bottomTabs,
                        ContainerView = new StackView(StackType.Horizontal),
                        ItemView = (val, idx) => new ButtonView(() => val)
                        {
                            IsChecked = () => selectedBottomTab == idx,
                            Clicked = () => selectedBottomTab = idx
                        }
                    }
                }
            }, Anchor.BottomLeft));

        // resources
        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                BackgroundColor = panelBackgroundColor,
                Children =
                {
                    new LabelView("Resources in stock:"),
                    new RepeaterView<ResourceQuantity>
                    {
                        Source = () => World.GetAllResources(ResourceMarker.Unmarked, true).Select(w => new ResourceQuantity(w.Key, w.Value)),
                        ContainerView = new StackView(StackType.Vertical),
                        ItemView = (rq, _) => getResourceRowView(false, rq.Resource,() => rq.Quantity),
                        EmptyView = new LabelView("None") { ForegroundColor = () => descriptionColor }
                    }
                }
            }, Anchor.TopRight));

        // timing info
        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                Children =
                {
                    new LabelView(() => $$"""
                        FPS: {{Math.Round(FrameData.Rate, 0, MidpointRounding.ToPositiveInfinity):0}}, Update: {{FrameData.UpdateTimePercentage * 100:0.00}}%, Render: {{FrameData.RenderTimePercentage * 100:0.00}}%
                        Draw calls: {{FrameData.DrawCallCount}}, Triangles: {{FrameData.TriangleCount}}, Lines: {{FrameData.LineCount}}
                        {{string.Join(Environment.NewLine, EcsCoordinator.SystemTimingInformation.Select((kvp, idx) => $"{kvp.Key}: {FrameData.GetCustomTimePercentage(idx) * 100:0.00}%"))}}
                        SwapBuffer: {{FrameData.GetCustomTimePercentage(EcsCoordinator.SystemsCount) * 100:0.00}}%
                        """)
                    {
                        FontSize = () => smallFontSize,
                        Padding = new(2),
                        ForegroundColor = () => Colors4.White,
                        BackgroundColor = panelBackgroundColor
                    },
                    new LabelView("PAUSED")
                    {
                        IsVisible = () => world.TimeSpeedUp == 0,
                        FontSize = () => smallFontSize,
                        Padding = new(2, 0),
                        ForegroundColor = () => Colors4.Red,
                    },
                }
            }, Anchor.TopLeft));

        // picker, has to be last
        gui.RootViewDescriptions.Add(new(
            new WindowView
            {
                IsVisible = () => isPickerVisible,
                Title = () => pickerTitle,
                Margin = () => new(pickerOffset.X, 0, 0, (int)(-pickerOffset.Y + HeightPercentage(100))),
                Child = new RepeaterView<string>
                {
                    Source = () => pickerValues,
                    ContainerView = new StackView(StackType.Vertical),
                    ItemView = (item, idx) => new ButtonView(() => item)
                    {
                        Clicked = () => pickerCompletionSource!.SetResult((item, idx)),
                    }
                }
            }, Anchor.BottomLeft));
    }
}
