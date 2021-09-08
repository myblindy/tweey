namespace Tweey;

class Program : GameWindow
{
    public Program() : base(
        new()
        {
            RenderFrequency = 60,
            UpdateFrequency = 60,
            IsMultiThreaded = false,
        }, new()
        {
            Profile = ContextProfile.Core,
            API = ContextAPI.OpenGL,
            APIVersion = new(4, 6),
            StartFocused = true,
            StartVisible = true,
            Size = new(800, 600),
            WindowState = WindowState.Maximized,
            Title = "TwEEY",
            Flags = ContextFlags.ForwardCompatible,
        })
    {
    }

    readonly World world = new(DiskLoader.Instance);
    WorldRenderer? worldRenderer;

    protected override unsafe void OnLoad()
    {
        VSync = VSyncMode.Off;

        // enable debug messages
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);

        GL.DebugMessageCallback((src, type, id, severity, len, msg, usr) =>
        {
            if (severity > DebugSeverity.DebugSeverityNotification)
                Console.WriteLine($"GL ERROR {Encoding.ASCII.GetString((byte*)msg, len)}, type: {type}, severity: {severity}, source: {src}");
        }, IntPtr.Zero);

        GL.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var villager = new Villager(world.Configuration.Data) { Location = new(5, 1) };
        world.PlaceEntity(villager);
        world.SelectedEntity = villager;
        world.PlaceEntity(new Villager(world.Configuration.Data) { Location = new(15, 20) });

        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["firewood"], 5)) { Location = new(3, 4) });
        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["iron"], 2)) { Location = new(7, 4) });

        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 3)) { Location = new(4, 5) });
        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["iron"], 4)) { Location = new(5, 5) });
        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 4)) { Location = new(6, 5) });

        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 24)) { Location = new(17, 20) });
        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 83)) { Location = new(19, 19) });
        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 67)) { Location = new(20, 19) });
        world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 67), new ResourceQuantity(world.Resources["iron"], 125)) { Location = new(20, 20) });
        world.PlaceEntity(Building.FromTemplate(world.BuildingTemplates["jumbo storage"], new(3, 20), new[] { world.Resources["wood"] }));

        worldRenderer = new(world);
        worldRenderer.Resize(Size.X, Size.Y);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, e.Width, e.Height);
        worldRenderer?.Resize(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        world.Update(args.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        worldRenderer!.Render(args.Time, UpdateTime, RenderTime);
        SwapBuffers();
    }

    static void Main()
    {
        using var program = new Program();
        program.Run();
    }
}
