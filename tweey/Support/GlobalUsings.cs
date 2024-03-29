﻿global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Collections.Immutable;
global using System.Collections.ObjectModel;
global using System.Diagnostics.CodeAnalysis;
global using System.IO;
global using System.IO.Compression;
global using System.Linq;
global using System.Numerics;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text.Json;
global using System.Text.Json.Serialization;

//global using SixLabors.ImageSharp;
//global using SixLabors.ImageSharp.PixelFormats;
//global using SixLabors.ImageSharp.Drawing.Processing;
//global using SixLabors.ImageSharp.Processing;
//global using SixLabors.Fonts;

global using Twee.Core;
global using Twee.Core.Ecs;
global using Twee.Core.JsonConverters;
global using Twee.Core.Support;
global using Twee.Core.Support.ObjectPools;
global using Twee.Ecs;
global using Twee.Renderer;
global using Twee.Renderer.Support;
global using Twee.Renderer.Shaders;
global using Twee.Renderer.Textures;
global using Twee.Renderer.VertexArrayObjects;

global using Tweey.Components;
global using Tweey.Gui;
global using Tweey.Gui.Base;
global using Tweey.Loaders;
global using Tweey.Support;
global using Tweey.Support.AI;
global using Tweey.Support.AI.HighLevelPlans;
global using Tweey.Support.AI.LowLevelPlans;
global using Tweey.WorldData;
global using OpenTK.Graphics;
global using OpenTK.Graphics.OpenGL;
global using OpenTK.Windowing.Common;
global using OpenTK.Windowing.Desktop;
global using OpenTK.Windowing.GraphicsLibraryFramework;

global using SuperLinq;

global using Vector2i = OpenTK.Mathematics.Vector2i;
global using Configuration = Tweey.Loaders.Configuration;
global using InputAction = OpenTK.Windowing.GraphicsLibraryFramework.InputAction;
global using MouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
global using KeyModifiers = OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers;
global using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
