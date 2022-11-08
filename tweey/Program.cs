namespace Tweey;

class Program : GameWindow
{
    public unsafe Program() : base(
        new()
        {
            RenderFrequency = 60,
            UpdateFrequency = 60,
        }, new()
        {
            Profile = ContextProfile.Core,
            API = ContextAPI.OpenGL,
            APIVersion = new(4, 6),
            StartFocused = true,
            StartVisible = true,
            WindowBorder = WindowBorder.Hidden,
            Size = new(800, 600),
            WindowState = WindowState.Maximized,
            Title = "TwEEY",
            Flags = ContextFlags.ForwardCompatible,
        })
    {
        // set the default render frequency to the primary monitor's refresh rate
        var monitor = GLFW.GetPrimaryMonitor();
        var videoMode = GLFW.GetVideoMode(monitor);
        if (videoMode != null)
            this.RenderFrequency = videoMode->RefreshRate;
    }

    readonly World world = new(DiskLoader.Instance);

    protected override unsafe void OnLoad()
    {
        VSync = VSyncMode.Off;

#if DEBUG
        // enable debug messages
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);

        GL.DebugMessageCallback((src, type, id, severity, len, msg, usr) =>
        {
            if (severity > DebugSeverity.DebugSeverityNotification)
                Console.WriteLine($"GL ERROR {new string((sbyte*)msg)}, type: {type}, severity: {severity}, source: {src}");
        }, IntPtr.Zero);
#endif

        GL.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);

        var villager = world.SelectedEntity = World.AddVillagerEntity("Sana", new(5, 1));
        World.AddVillagerEntity("Momo", new(15, 20));

        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["bread"], 100)), new(3, 3));

        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["firewood"], 5)), new(3, 4));
        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["iron"], 2)), new(7, 4));

        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 3)), new(4, 5));
        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["iron"], 4)), new(5, 5));
        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 4)), new(6, 5));
        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["firewood"], 55)), new(7, 7));

        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 24)), new(17, 20));
        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["stone"], 120)), new(17, 21));
        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 83)), new(19, 19));
        world.AddResourceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 67)), new(20, 19));
        world.AddResourceEntity(new ResourceBucket(
            new ResourceQuantity(world.Resources["wood"], 67),
            new ResourceQuantity(world.Resources["iron"], 125)),
            new(20, 20));

        World.PlantForest(world.TreeTemplates["pine"], new(3, 20), 6, .9f, .2f);
        World.PlantForest(world.TreeTemplates["pine"], new(40, 12), 12, .8f, .1f);

        World.AddBuildingEntity(world.BuildingTemplates["well"], new(8, 12), false);

        EcsCoordinator.ConstructSetPlacedResourceAtlasEntrySystem(() => new());
        EcsCoordinator.ConstructRenderSystem(() => new(world));
        EcsCoordinator.SendResizeMessageToRenderSystem(Size.X, Size.Y);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        if (EcsCoordinator.IsRenderSystemConstructed)
            EcsCoordinator.SendResizeMessageToRenderSystem(e.Width, e.Height);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        var position = MousePosition.ToVector2i();
        if (!EcsCoordinator.SendMouseEventMessageToRenderSystem(position, e.Action, e.Button, e.Modifiers))
            world.MouseEvent(position, world.GetWorldLocationFromScreenPoint(position), e.Action, e.Button, e.Modifiers);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        var position = MousePosition.ToVector2i();
        if (!EcsCoordinator.SendMouseEventMessageToRenderSystem(position, e.Action, e.Button, e.Modifiers))
            world.MouseEvent(position, world.GetWorldLocationFromScreenPoint(position), e.Action, e.Button, e.Modifiers);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        var position = MousePosition.ToVector2i();
        world.MouseEvent(position, world.GetWorldLocationFromScreenPoint(position));
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e) =>
        world.KeyEvent(e.IsRepeat ? InputAction.Repeat : InputAction.Press, e.Key, e.ScanCode,
            (e.Control ? KeyModifiers.Control : 0) | (e.Shift ? KeyModifiers.Shift : 0) | (e.Shift ? KeyModifiers.Alt : 0));

    protected override void OnKeyUp(KeyboardKeyEventArgs e) =>
        world.KeyEvent(InputAction.Release, e.Key, e.ScanCode, (e.Control ? KeyModifiers.Control : 0) | (e.Shift ? KeyModifiers.Shift : 0) | (e.Shift ? KeyModifiers.Alt : 0));

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        world.Update(args.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        EcsCoordinator.RunSystems(args.Time, UpdateTime, RenderTime);
        SwapBuffers();
    }

    static unsafe void Main()
    {
        using var program = new Program();
        program.Run();
    }
}
