using Godot;
using System;

public partial class RenderOptionsPanel : Control
{
    [Export] private Button pixels_down_but;
    [Export] private Button pixels_up_but;

    public override void _Ready()
    {
        pixels_down_but.Pressed += () =>
        {
            GD.Print("down!");
        };

        pixels_up_but.Pressed += () =>
        {
            GD.Print("up!");
        };
    }
}
