﻿using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using Tweey.Actors;
using Tweey.Support;

namespace Tweey.Renderer
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

        float pixelZoom = 26;

        static readonly Vector4 colorWhite = new(1, 1, 1, 1);

        public void Render(double _)
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
                        worldQuad(Box2.FromCornerSize(building.Location, building.Width, building.Height), building.Color);
                        break;
                    case ResourceBucket resourceBucket:
                        var resWeight = resourceBucket.Resources.Sum(rq => rq.Weight);
                        worldQuad(Box2.FromCornerSize(resourceBucket.Location, 1, 1), colorWhite);
                        foreach (var resQ in resourceBucket.Resources)
                        {
                            var percentage = (float)(resWeight / world.Configuration.Data.GroundStackMaximumWeight);
                            worldQuad(Box2.FromCornerSize(new(resourceBucket.Location.X + (1 - percentage) / 2, resourceBucket.Location.Y), percentage, 1), resQ.Resource.Color);
                            resWeight -= resQ.Weight;
                        }
                        break;
                    case Villager villager:
                        worldQuad(Box2.FromCornerSize(villager.Location, 1, 1), colorWhite);
                        break;
                }

            shaderProgram.Use();
            windowUbo.Bind(windowUboBindingPoint);
            vaoGui.Draw(PrimitiveType.Triangles);
        }
    }
}
