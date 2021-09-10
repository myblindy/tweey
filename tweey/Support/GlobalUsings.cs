global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Collections.Immutable;
global using System.Collections.ObjectModel;
global using System.Linq;
global using System.Text;
global using System.Numerics;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text.Json;
global using System.Text.Json.Serialization;

global using SixLabors.ImageSharp.PixelFormats;
global using SixLabors.ImageSharp;

global using Tweey.Actors;
global using Tweey.Actors.Interfaces;
global using Tweey.Gui;
global using Tweey.Loaders;
global using Tweey.Renderer;
global using Tweey.Support;
global using Tweey.WorldData;

global using OpenTK.Graphics;
global using OpenTK.Graphics.OpenGL;
global using OpenTK.Windowing.Common;
global using OpenTK.Windowing.Desktop;

global using static MoreLinq.Extensions.ForEachExtension;

global using Vector2i = OpenTK.Mathematics.Vector2i;
global using Configuration = Tweey.Loaders.Configuration;
global using InputAction = OpenTK.Windowing.GraphicsLibraryFramework.InputAction;
global using MouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
global using KeyModifiers = OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers;