using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using tweey.Actors;
using tweey.Support;

namespace tweey.Renderer
{
    class WorldRenderer
    {
        readonly World world;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct WindowUbo
        {
            public Vector2 WindowSize;
        }

        readonly UniformBufferObject<WindowUbo> windowUbo = new();
        const int windowUboBindingPoint = 1;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct GuiVertex
        {
            public Vector2 Location;
            public Vector4 Color;

            public GuiVertex(Vector2 location, Vector4 color) =>
                (Location, Color) = (location, color);
        }

        readonly VertexArrayObject<GuiVertex, Nothing> vaoGui = new(false, 1024, 0);
        readonly ShaderProgram shaderProgram = new("gui");

        public WorldRenderer(World world)
        {
            this.world = world;
            shaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        }

        public void Resize(int width, int height)
        {
            windowUbo.Data.WindowSize = new Vector2(width, height);
            windowUbo.Update();
        }

        float pixelZoom = 10f;

        public void Render(double deltaTime)
        {
            vaoGui.Vertices.Clear();

            void worldQuad(Box2 box, Vector4 color)
            {
                vaoGui.Vertices.Add(new(box.TopLeft * pixelZoom, color));
                vaoGui.Vertices.Add(new(box.BottomRight * pixelZoom, color));
                vaoGui.Vertices.Add(new(new(box.Right * pixelZoom, box.Top * pixelZoom), color));

                vaoGui.Vertices.Add(new(new(box.Left * pixelZoom, box.Bottom * pixelZoom), color));
                vaoGui.Vertices.Add(new(box.BottomRight * pixelZoom, color));
                vaoGui.Vertices.Add(new(box.TopLeft * pixelZoom, color));
            }

            foreach (var entity in world.PlacedEntities)
                switch (entity)
                {
                    case Building building:
                        worldQuad(Box2.FromCenterSize(building.Location, building.Width, building.Height), new Vector4(1, 1, 1, 1));
                        break;
                }

            shaderProgram.Use();
            windowUbo.Bind(windowUboBindingPoint);
            vaoGui.Draw(PrimitiveType.Triangles);
        }
    }
}
