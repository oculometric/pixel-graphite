using Godot;
using System;

public partial class LightEditController : Node3D
{
	[Export] public MainSceneController scene_controller;

    [Export] private Light3D sun;
    [Export] private float granular_pan_speed = 4.0f;
    [Export] private float snap_angle = 45.0f;

    public void SetEditingEnabled(bool enabled)
	{
		if (enabled)
		{
			SetProcessUnhandledInput(true);
		}
		else
		{
			SetProcessUnhandledInput(false);
		}
	}

    // TODO: light UI that describes the angles of the light
    // TODO: indicator arrow

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.Shift) && IsProcessingUnhandledInput())
        {
            float rot_y = ((Input.IsKeyPressed(Key.D) ? 1 : 0) - (Input.IsKeyPressed(Key.A) ? 1 : 0)) * (float)delta * granular_pan_speed;
            float rot_x = ((Input.IsKeyPressed(Key.S) ? 1 : 0) - (Input.IsKeyPressed(Key.W) ? 1 : 0)) * (float)delta * granular_pan_speed;
            RotateUpAxis(rot_y);
            RotateRightAxis(rot_x);
        }
    }

    private void RotateUpAxis(float angle)
    {
        sun.RotateY(Mathf.DegToRad(angle));
    }

    private void RotateRightAxis(float angle)
    {
        sun.Rotate(sun.GlobalBasis.Column0, Mathf.DegToRad(angle));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            InputEventKey key = @event as InputEventKey;
            if (!Input.IsKeyPressed(Key.Shift))
            {
                if (key.IsReleased())
                {
                    switch (key.Keycode)
                    {
                        case Key.A: RotateUpAxis(-snap_angle); break;
                        case Key.D: RotateUpAxis(snap_angle); break;
                        case Key.W: RotateRightAxis(-snap_angle); break;
                        case Key.S: RotateRightAxis(snap_angle); break;
                    }
                }
            }
            
            if (key.IsPressed())
            {
                switch (key.Keycode)
                {
                    case Key.Q: sun.Visible = !sun.Visible; break;
                    case Key.E: GetWorld3D().Environment.AmbientLightEnergy = GetWorld3D().Environment.AmbientLightEnergy > 0.0f ? 0.0f : 1.0f; break;
                    case Key.R: sun.ShadowEnabled = !sun.ShadowEnabled; break;
                }
            }
        }
    }
}
