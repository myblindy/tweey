namespace Tweey.Systems;

[EcsSystem(Archetypes.Render)]
partial class RenderSystem
{
    readonly World world;
    readonly ShaderPrograms shaderPrograms = new();
    readonly GrowableTextureAtlas3D atlas;
    readonly FontRenderer fontRenderer;

    readonly UniformBufferObject<WindowUbo> windowUbo = new();
    const int windowUboBindingPoint = 1;

    readonly StreamingVertexArrayObject<GuiVertex> guiVAO = new((int)RenderLayer.MaximumCount);
    readonly ShaderProgram guiShaderProgram;
    readonly ShaderProgram guiLightMapShaderProgram;
    readonly GuiSpace gui = new();

    readonly AtlasEntry markForHarvestAtlasEntry;
    readonly AtlasEntry blankAtlasEntry;

    readonly StaticVertexArrayObject<LightMapFBVertex> lightMapFBVao =
        new(new LightMapFBVertex[]
        {
            new(new(-1, -1), new (0, 0)),
            new(new(1, -1), new (1, 0)),
            new(new(-1, 1), new (0, 1)),

            new(new(-1, 1), new (0, 1)),
            new(new(1, -1), new (1, 0)),
            new(new(1, 1), new (1, 1)),
        });
    readonly ShaderProgram lightMapFBShaderProgram;
    readonly UniformBufferObject<LightMapFBUbo> lightMapFBUbo = new();

    Texture2D lightMapOcclusionTexture = null!;
    FrameBuffer lightMapOcclusionFrameBuffer = null!;
    readonly StreamingVertexArrayObject<LightMapOcclusionFBVertex> lightMapOcclusionVAO = new();
    readonly ShaderProgram lightMapOcclusionShaderProgram;
    const int lightMapOcclusionTextureDivisor = 2;
    readonly Texture2D lightMapOcclusionCircleTexture;

    const int lightsUboBindingPoint = 3;
    Texture2D lightMapTexture = null!;
    FrameBuffer lightMapFrameBuffer = null!;

    public RenderSystem(World world)
    {
        guiShaderProgram = new(shaderPrograms, DiskLoader.Instance.VFS, "gui");
        guiLightMapShaderProgram = new(shaderPrograms, DiskLoader.Instance.VFS, "gui-lightmap");

        this.world = world;

        var maxTextureSize = Math.Min(2048, GraphicsEngine.MaxTextureSize);
        atlas = new(maxTextureSize, maxTextureSize, 1, DiskLoader.Instance.VFS);
        fontRenderer = new(atlas, DiskLoader.Instance.VFS);

        guiShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        guiShaderProgram.Uniform("atlasSampler", 0);

        guiLightMapShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        guiLightMapShaderProgram.Uniform("atlasSampler", 0);
        guiLightMapShaderProgram.Uniform("lightMapSampler", 1);

        blankAtlasEntry = atlas[GrowableTextureAtlas3D.BlankName];
        markForHarvestAtlasEntry = atlas["Data/Misc/mark-for-harvest-overlay.png"];

        using (var lightMapOcclusionCircleTextureStream = DiskLoader.Instance.VFS.OpenRead(@"Data\Misc\large-circle.png")!)
            lightMapOcclusionCircleTexture = new(lightMapOcclusionCircleTextureStream,
                SizedInternalFormat.R8, minFilter: TextureMinFilter.NearestMipmapNearest, magFilter: TextureMagFilter.Nearest);

        lightMapFBShaderProgram = new(shaderPrograms, DiskLoader.Instance.VFS, "lightmap");
        lightMapOcclusionShaderProgram = new(shaderPrograms, DiskLoader.Instance.VFS, "lightmap-occlusion");

        lightMapFBShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        lightMapFBShaderProgram.UniformBlockBind("ubo_lights", lightsUboBindingPoint);
        lightMapFBShaderProgram.Uniform("occlusionSampler", 0);

        lightMapOcclusionShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        lightMapOcclusionShaderProgram.Uniform("circleSampler", 0);

        InitializeGui();

        windowUbo.Bind(windowUboBindingPoint);
        lightMapFBUbo.Bind(lightsUboBindingPoint);
    }

    [Message]
    public void ResizeMessage(int width, int height)
    {
        if (width <= 0 || height <= 0) return;

        windowUbo.Data.WindowSize = new(width, height);
        windowUbo.UploadData();

        defaultFontSize = HeightPercentage(1.6f);
        largeFontSize = defaultFontSize * 1.2f;
        smallFontSize = defaultFontSize * 0.8f;

        lightMapFrameBuffer?.Dispose();
        lightMapTexture?.Dispose();
        lightMapTexture = new(width, height, SizedInternalFormat.Rgba8, minFilter: TextureMinFilter.Nearest, magFilter: TextureMagFilter.Nearest);
        lightMapFrameBuffer = new(new[] { lightMapTexture });

        lightMapOcclusionFrameBuffer?.Dispose();
        lightMapOcclusionTexture?.Dispose();
        lightMapOcclusionTexture = new(width / lightMapOcclusionTextureDivisor, height / lightMapOcclusionTextureDivisor, SizedInternalFormat.R8);
        lightMapOcclusionFrameBuffer = new(new[] { lightMapOcclusionTexture });
    }

    unsafe void RenderLightMapToFrameBuffer(in Box2 worldViewBox, bool useTorches)
    {
        // setup the occlusion map for rendering and build the occlusions
        void markOcclusionBox(in Box2 box, bool circle = false, float scale = 1f)
        {
            var zoom = world.Zoom;
            var uvHalf = new Vector2(.5f);         // the center of the circle texture is white, use that for the box case

            var center = box.Center - world.Offset;
            var rx = box.Size.X / 2 * scale;
            var ry = box.Size.Y / 2 * scale;

            var vertices = lightMapOcclusionVAO.LayerVertices[0];
            vertices.Add(new((center + new Vector2(-rx, ry)) * zoom, circle ? new(0, 0) : uvHalf));
            vertices.Add(new((center + new Vector2(rx, -ry)) * zoom, circle ? new(1, 1) : uvHalf));
            vertices.Add(new((center + new Vector2(rx, ry)) * zoom, circle ? new(1, 0) : uvHalf));

            vertices.Add(new((center + new Vector2(-rx, -ry)) * zoom, circle ? new(0, 1) : uvHalf));
            vertices.Add(new((center + new Vector2(rx, -ry)) * zoom, circle ? new(1, 1) : uvHalf));
            vertices.Add(new((center + new Vector2(-rx, ry)) * zoom, circle ? new(0, 0) : uvHalf));
        }

        IterateRenderPartitionByLocationComponents(worldViewBox, (in IterationResult w) =>
        {
            if (w.Entity.HasBuildingComponent() && !w.Entity.GetBuildingComponent().IsBuilt) return;

            if (w.RenderableComponent.OcclusionScale > 0)
                markOcclusionBox(w.LocationComponent.Box, w.RenderableComponent.OcclusionCircle, w.RenderableComponent.OcclusionScale);
        });

        lightMapOcclusionVAO.UploadNewData(Range.All);

        lightMapOcclusionFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GraphicsEngine.Viewport(0, 0, (int)windowUbo.Data.WindowSize.X / lightMapOcclusionTextureDivisor, (int)windowUbo.Data.WindowSize.Y / lightMapOcclusionTextureDivisor);
        GraphicsEngine.Clear();
        lightMapOcclusionShaderProgram.Use();
        lightMapOcclusionCircleTexture.Bind(0);
        GraphicsEngine.BlendAdditive();            // no alpha channel, use additive blending
        lightMapOcclusionVAO.Draw(PrimitiveType.Triangles);

        // setup the light map for rendering
        // upload the light data to the shader
        var lightCount = 0;

        // setup the re-callable engine to render the light maps
        lightMapFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GraphicsEngine.Viewport(0, 0, (int)windowUbo.Data.WindowSize.X, (int)windowUbo.Data.WindowSize.Y);
        GraphicsEngine.Clear();
        lightMapFBShaderProgram.Use();
        lightMapOcclusionTexture.Bind(0);
        GraphicsEngine.BlendAdditive();

        void renderLights()
        {
            if (lightCount == 0) return;

            for (int idx = lightCount; idx < LightMapFBUbo.MaxLightCount; ++idx)
                lightMapFBUbo.Data[idx].ClearToInvalid();

            lightMapFBUbo.UploadData();

            // render the light map
            lightMapFBVao.Draw(PrimitiveType.Triangles);
        }

        void addLight(Vector2 location, float range, Vector3 color, Vector2 angleMinMax)
        {
            lightMapFBUbo.Data[lightCount++] = new(location, range, color, angleMinMax);
            if (lightCount == LightMapFBUbo.MaxLightCount)
            {
                renderLights();
                lightCount = 0;
            }
        }

        Vector2 getAngleMinMaxFromHeading(double heading, double coneAngle) =>
            new((float)((-heading - coneAngle + 2.25) % 1.0), (float)((-heading + coneAngle + 2.25) % 1.0));

        // call the engine once for each light
        IterateRenderPartitionByLocationComponents(worldViewBox, (in IterationResult w) =>
        {
            if (w.RenderableComponent.LightEmission.W == 0) return;
            if (!useTorches && w.Entity.HasVillagerComponent()) return;
            if (w.Entity.HasBuildingComponent() && !w.Entity.GetBuildingComponent().IsBuilt) return;

            addLight((w.LocationComponent.Box.Center - world.Offset) * world.Zoom, w.RenderableComponent.LightRange * world.Zoom,
                w.RenderableComponent.LightEmission.GetXYZ(), w.RenderableComponent.LightFullCircle ? LightMapFBUbo.Light.FullAngle :
                    getAngleMinMaxFromHeading(w.Entity.GetHeadingComponent().Heading, w.RenderableComponent.LightAngleRadius));
        });

        if (world.DebugShowLightAtMouse)
        {
            var totalTimeSec = (float)world.TotalRealTime.TotalSeconds;
            addLight(new Vector2(world.MouseScreenPosition.X, world.MouseScreenPosition.Y) - world.Offset * world.Zoom, 16 * world.Zoom,
                new(MathF.Sin(totalTimeSec / 2f) / 2 + 1, MathF.Sin(totalTimeSec / 4f) / 2 + 1, MathF.Sin(totalTimeSec / 6f) / 2 + 1),
                LightMapFBUbo.Light.FullAngle);
        }

        renderLights();
    }

    void RenderZone(in Box2 box, ZoneType zoneType, bool error, bool showGrid, bool showSizes)
    {
        const float zoneBackgroundAlpha = .6f;
        ScreenFillQuad(RenderLayer.Zone, box, (error, zoneType) switch
        {
            (false, ZoneType.Grow) => world.Configuration.Data.ZoneGrowColor.ToVector4(zoneBackgroundAlpha),
            (false, ZoneType.Storage) => world.Configuration.Data.ZoneStorageColor.ToVector4(zoneBackgroundAlpha),
            (_, ZoneType.MarkHarvest) => world.Configuration.Data.ZoneHarvestColor.ToVector4(zoneBackgroundAlpha),
            (true, _) => world.Configuration.Data.ZoneErrorColor.ToVector4(zoneBackgroundAlpha),
            _ => throw new NotImplementedException()
        }, blankAtlasEntry);

        // grid
        if (showGrid)
        {
            var cellAtlas = atlas["Data/Misc/zonecell.png"];
            for (var y = box.Top; y <= box.Bottom; ++y)
                for (var x = box.Left; x <= box.Right; ++x)
                    ScreenFillQuad(RenderLayer.Zone, Box2.FromCornerSize(x, y, 1, 1), Colors4.White, cellAtlas);
        }

        // sizes
        if (showSizes && box.Size is { X: > 1, Y: > 1 })
        {
            var screenBox = Box2.FromCornerSize((box.TopLeft + world.Offset) * world.Zoom, box.Size * world.Zoom);
            ScreenString(RenderLayer.Gui, box.Size.X.ToString(), new() { Size = largeFontSize }, screenBox, Colors4.White, panelBackgroundColor, HorizontalAlignment.Center);
            ScreenString(RenderLayer.Gui, box.Size.Y.ToString(), new() { Size = largeFontSize },
                screenBox.WithOffset(new Vector2(0, box.Size.Y * world.Zoom / 2 - largeFontSize / 2)),
                Colors4.White, panelBackgroundColor);
        }
    }

    void RenderBuildingSite(in Box2 box, in BuildingComponent buildingComponent)
    {
        var worldBox = Box2.FromCornerSize((box.TopLeft - world.Offset) * world.Zoom, new(world.Zoom));
        ScreenFillFrame(RenderLayer.BelowPawns, worldBox, "BuildingSite", world.Zoom / 3, FrameType.NoEdges | FrameType.NoBackground);

        var percentageFilled = 1 - buildingComponent.BuildWorkTicks / buildingComponent.Template.BuildWorkTicks;
        const int subDivisions = 2;
        var totalCells = box.Size * subDivisions;

        for (int cellsToFill = (int)Math.Ceiling(totalCells.X * totalCells.Y * percentageFilled), row = 0, col = 0; cellsToFill > 0; --cellsToFill)
        {
            ScreenStrokeQuad(RenderLayer.Gui,
                Box2.FromCornerSize(worldBox.BottomRight - new Vector2(col + 1, row + 1) * world.Zoom / subDivisions, new(world.Zoom / subDivisions)).WithExpand(new Vector2(-2)),
                2, Colors4.DarkGray, blankAtlasEntry);

            if (++col == totalCells.Y)
                (row, col) = (row + 1, 0);
        }
    }

    public partial void Run()
    {
        var worldViewBox = Box2.FromCornerSize(world.Offset, windowUbo.Data.WindowSize / world.Zoom);

        // time of day ambient light calculation
        var h = world.WorldTime.TimeOfDay.TotalHours;
        var ambientColorPercentage = (float)(h < 12 ? h / 12 : 1 - (h - 12) / 12);
        var useTorches = h < 10 || h > 20;

        // render lightmap to texture
        RenderLightMapToFrameBuffer(worldViewBox, useTorches);

        // render to screen
        GraphicsEngine.UnbindFrameBuffer();

        // render the background (tri0)
        var normalizedGrassOffset = world.Offset.ToVector2i().ToNumericsVector2();

        if (world.TerrainCells is { })
            for (int y = -1; y < windowUbo.Data.WindowSize.Y / world.Zoom + 1; ++y)
                for (int x = -1; x < windowUbo.Data.WindowSize.X / world.Zoom + 1; ++x)
                    if ((int)(x + normalizedGrassOffset.X) is { } xIdx && (int)(y + normalizedGrassOffset.Y) is { } yIdx
                        && xIdx >= 0 && yIdx >= 0 && xIdx < world.TerrainCells.GetLength(0) && yIdx < world.TerrainCells.GetLength(1))
                    {
                        ScreenFillQuad(RenderLayer.Ground, Box2.FromCornerSize(new Vector2(x, y) + normalizedGrassOffset, 1, 1), Colors4.White,
                            atlas[world.TerrainCells[(int)(x + normalizedGrassOffset.X), (int)(y + normalizedGrassOffset.Y)].TileFileName!]);
                    }

        // store the actual entities' vertices(tri0)
        IterateRenderPartitionByLocationComponents(worldViewBox, (in IterationResult w) =>
        {
            if (w.Entity.HasBuildingComponent())
            {
                ref var buildingComponent = ref w.Entity.GetBuildingComponent();
                if (!buildingComponent.IsBuilt)
                {
                    RenderBuildingSite(w.LocationComponent.Box, buildingComponent);
                    return;
                }
            }

            if (w.Entity.HasPlantComponent())
            {
                ref var plantComponent = ref w.Entity.GetPlantComponent();
                ScreenFillQuad(RenderLayer.BelowPawns, w.LocationComponent.Box, atlas[plantComponent.Template.GetImageFileName(plantComponent.GetGrowth(world))]);
            }
            else if (w.RenderableComponent.AtlasEntryName is { } atlasEntryName)
            {
                if (w.Entity.HasInventoryComponent() && w.Entity.HasResourceComponent() && w.Entity.GetInventoryComponent().Inventory.IsEmpty(ResourceMarker.Unmarked))
                    return;
                ScreenFillQuad(w.Entity.HasVillagerComponent() ? RenderLayer.Pawn : RenderLayer.BelowPawns, w.LocationComponent.Box, atlas[atlasEntryName]);
            }
            else if (w.Entity.HasZoneComponent())
            {
                ref var zoneComponent = ref w.Entity.GetZoneComponent();
                RenderZone(w.LocationComponent.Box, zoneComponent.Type, false, false, false);
            }

            if (w.Entity.HasMarkForHarvestComponent())
                ScreenFillQuad(RenderLayer.Gui, w.LocationComponent.Box, markForHarvestAtlasEntry);
        });

        // store the ai plan targets' vertices (lines)
        if (world.ShowDetails)
            EcsCoordinator.IterateWorkerArchetype((in EcsCoordinator.WorkerIterationResult w) =>
            {
                if (w.WorkerComponent.CurrentLowLevelPlan is AILowLevelPlanWithTargetEntity aiLowLevelPlanWithTargetEntity
                    && aiLowLevelPlanWithTargetEntity.TargetEntity is { } targetEntity)
                {
                    ScreenLine(RenderLayer.Gui, w.LocationComponent.Box,
                        aiLowLevelPlanWithTargetEntity.TargetEntity!.Value.GetLocationComponent().Box, 1.5f, Colors4.Yellow);
                }
            });
        else if (world.SelectedEntity.HasValue && world.SelectedEntity.Value.HasWorkerComponent()
            && world.SelectedEntity.Value.GetWorkerComponent().CurrentLowLevelPlan is AILowLevelPlanWithTargetEntity { } aiLowLevelPlanWithTargetEntity
            && aiLowLevelPlanWithTargetEntity.TargetEntity is { } targetEntity)
        {
            ScreenLine(RenderLayer.Gui, world.SelectedEntity.Value.GetLocationComponent().Box,
                targetEntity.GetLocationComponent().Box, 1.5f, Colors4.Yellow);
        }

        // selection box (lines)
        if (world.SelectedEntity is { } entity)
            ScreenStrokeQuad(RenderLayer.Gui, ConvertWorldToScreenBox(entity.GetLocationComponent().Box), 1f, Colors4.White, blankAtlasEntry);

        // render top layer (tri2)
        EcsCoordinator.IterateVillagerArchetype((in EcsCoordinator.VillagerIterationResult w) =>
        {
            ScreenString(RenderLayer.Gui, w.IdentityComponent.Name, new() { Size = 16 },
                new Vector2((w.LocationComponent.Box.Left + .5f - world.Offset.X) * world.Zoom, (w.LocationComponent.Box.Top - world.Offset.Y) * world.Zoom - 20),
                Colors4.White, new(0, 0, 0, .4f), HorizontalAlignment.Center);
            //if (world.ShowDetails)
            //    ScreenString(villager.AIPlan?.Description, new() { Size = 13 },
            //        new Vector2((villager.InterpolatedLocation.X + .5f - world.Offset.X) * world.Zoom, (villager.InterpolatedLocation.Y + 1 - world.Offset.Y) * world.Zoom),
            //        Colors4.White, new(0, 0, 0, .4f), HorizontalAlignment.Center);
        });

        // building template
        if (world.CurrentWorldTemplate.BuildingTemplate is not null)
        {
            var box = Box2.FromCornerSize(world.MouseWorldPosition.ToVector2i(), world.CurrentWorldTemplate.BuildingTemplate.Width, world.CurrentWorldTemplate.BuildingTemplate.Height);
            ScreenFillQuad(RenderLayer.BelowPawns, box, World.IsBoxFreeOfBuildings(box) ? Colors4.Lime : Colors4.Red, atlas[world.CurrentWorldTemplate.BuildingTemplate.ImageFileName]);
        }

        // zone template
        if (world.CurrentWorldTemplate.ZoneType is not null && world.CurrentZoneStartPoint is not null
            && Box2.FromCornerSize(world.CurrentZoneStartPoint.Value, (world.MouseWorldPosition - world.CurrentZoneStartPoint.Value.ToNumericsVector2() + Vector2.One).ToVector2i()) is { } zoneBox)
        {
            RenderZone(zoneBox, world.CurrentWorldTemplate.ZoneType.Value, !World.IsBoxFreeOfBuildings(zoneBox), true, true);
        }

        // render gui (tri2)
        RenderGui();

        // draw the world (with light mapping)
        guiLightMapShaderProgram.Use();
        guiLightMapShaderProgram.Uniform("ambientColor",
            (world.Configuration.Data.MidDayColor * ambientColorPercentage + world.Configuration.Data.MidNightColor * (1 - ambientColorPercentage)).ToVector4(1));
        atlas.Bind(0);
        lightMapTexture.Bind(1);

        GraphicsEngine.Viewport(0, 0, (int)windowUbo.Data.WindowSize.X, (int)windowUbo.Data.WindowSize.Y);
        GraphicsEngine.BlendNormalAlpha();

        guiVAO.UploadNewData(..^1);
        guiVAO.Draw(PrimitiveType.Triangles);

        // draw the gui overlays, which shouldn't be light mapped
        guiShaderProgram.Use();
        guiVAO.UploadNewData(Range.All);
        guiVAO.Draw(PrimitiveType.Triangles);
    }
}
