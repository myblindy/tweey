using AutoMapper;
using OpenTK.Graphics.OpenGL4;
//using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Numerics;
using System.Text;
using tweey.Actors;
using tweey.Loaders;
using tweey.Renderer;

namespace tweey
{
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
                Profile = ContextProfile.Any,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 6),
                StartFocused = true,
                StartVisible = true,
                Size = new OpenTK.Mathematics.Vector2i(800, 600),
                Title = "TwEEY",
                Flags = ContextFlags.ForwardCompatible,
            })
        {
        }

        World world;
        WorldRenderer worldRenderer;

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

            GL.ClipControl(ClipOrigin.UpperLeft, ClipDepthMode.ZeroToOne);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            world = new World(DiskLoader.Instance);
            world.PlaceEntity(new ResourceBucket(new ResourceQuantity(world.Resources["wood"], 20)) { Location = new(25, 20) });
            world.PlaceEntity(Building.FromTemplate(world.BuildingTemplates["jumbo storage"], new(3, 20), new[] { world.Resources["wood"] }));
            world.PlaceEntity(new Villager { Location = new(1, 1) });

            worldRenderer = new(world);
            worldRenderer.Resize(Size.X, Size.Y);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
            worldRenderer.Resize(e.Width, e.Height);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            worldRenderer.Render(args.Time);
            SwapBuffers();
        }

        static void Main()
        {
            using var program = new Program();
            program.Run();
        }
    }
}
