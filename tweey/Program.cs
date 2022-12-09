using System.Diagnostics;

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

        FrameData.Init(EcsCoordinator.SystemsCount + 1);
        EcsCoordinator.ConstructPartitions(new(400, 400), world.Zoom);
        world.GenerateMap(400, 400);

        var villager = world.SelectedEntity = world.AddVillagerEntity("Sana", new(200, 200));
        //world.AddVillagerEntity("Momo", new(215, 220));
        world.Offset = new(190, 190);

        EcsCoordinator.ConstructFarmSystem(() => new(world));
        EcsCoordinator.ConstructAISystem(() => new(world));
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
        EcsCoordinator.RunSystems();

        var sw = Stopwatch.StartNew();
        SwapBuffers();
        FrameData.NewCustomTime(EcsCoordinator.SystemsCount, sw.Elapsed);

        for (int i = 0; i < EcsCoordinator.SystemTimingInformation.Count; i++)
            FrameData.NewCustomTime(i, EcsCoordinator.SystemTimingInformation.ElementAt(i).Value);
        FrameData.NewFrame(TimeSpan.FromSeconds(args.Time), TimeSpan.FromSeconds(UpdateTime), TimeSpan.FromSeconds(RenderTime));
    }

    static unsafe void Main()
    {
        using var program = new Program();
        program.Run();
    }
}
