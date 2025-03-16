using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class CameraController : Node3D
{
	[Export] Node3D camera_spin;
	[Export] Node3D camera_pitch;
	[Export] Camera3D camera;

	[Export] float max_pan_velocity = 16.0f;

	Vector2 pan_velocity = Vector2.Zero;
	Vector2 mouse_delta = Vector2.Zero;

	public override void _PhysicsProcess(double delta)
	{
		ProcessCameraInput((float)delta);
	}

	private void ProcessCameraInput(float delta)
	{
		camera_spin.GlobalPosition = GlobalPosition;

		camera_spin.RotateY(pan_velocity.X * (float)delta);
		camera_pitch.Translate(Vector3.Up * -pan_velocity.Y * 5.0f * (float)delta);

		Vector2 velocity_target = (mouse_delta / (float)delta).Clamp(-max_pan_velocity, max_pan_velocity);
		Vector2 velocity_difference = (velocity_target - pan_velocity) * (1.0f - ((float)delta * 6.0f));
		pan_velocity = velocity_target - velocity_difference;

		mouse_delta = Vector2.Zero;
	}

	public override void _Ready()
	{
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion motion = @event as InputEventMouseMotion;

			if (Input.IsMouseButtonPressed(MouseButton.Left))
				mouse_delta += motion.Relative * -0.0016f;
		}
	}
}
