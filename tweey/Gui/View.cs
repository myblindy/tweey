﻿namespace Tweey.Gui;

public record struct Thickness(int Left, int Top, int Right, int Bottom)
{
    public Thickness(int all) : this(all, all, all, all) { }
    public Thickness(int x, int y) : this(x, y, x, y) { }
}

public abstract class View
{
    public Func<bool>? Visible { get; set; }
    public Thickness Padding { get; set; }
    public Func<int>? MinWidth { get; set; }
    public Func<int>? MinHeight { get; set; }
    public Vector4 BackgroundColor { get; set; } = Colors.Transparent;
}