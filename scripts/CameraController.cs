using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class CameraController : Node3D
{
	[Export] Node3D camera_spin;
	[Export] Node3D camera_pitch;
	[Export] Camera3D camera;

	[Export] VoxelEditController vox_controller;

	[Export] float max_pan_velocity = 16.0f;

	Vector2 pan_velocity = Vector2.Zero;
	Vector2 mouse_delta = Vector2.Zero;
	Vector3 lerp_target = Vector3.Zero;

	public override void _PhysicsProcess(double delta)
	{
		GetWindow().Title = "pixel graphite (" + Engine.GetFramesPerSecond().ToString("000.0") + " fps)";

		ProcessCameraInput((float)delta);
		Vector3 towards_target = lerp_target - GlobalPosition;
		if (towards_target.LengthSquared() < 0.001f)
		{
			GlobalPosition = lerp_target;
			return;
		}
		float length = towards_target.Length();
		Vector3 direction = towards_target.Normalized();
		GlobalPosition = GlobalPosition + (direction * Mathf.Clamp(((10.0f * Mathf.Log(length + 1.0f)) + 0.1f) * (float)delta * 1.0f, 0.0f, length));
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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion motion = @event as InputEventMouseMotion;

			if (Input.IsMouseButtonPressed(MouseButton.Left))
				mouse_delta += motion.Relative * -0.0016f;
		}
		if (@event is InputEventMouseButton)
		{
			InputEventMouseButton button = @event as InputEventMouseButton;
			if (button.IsPressed() && button.ButtonIndex == MouseButton.Right)
			{
				Vector3I cell = vox_controller.GetHighlightedCell(true);
				GlobalPosition = camera_pitch.GlobalPosition;
				camera_pitch.GlobalPosition = GlobalPosition;
				lerp_target = new Vector3(cell.X, cell.Y, cell.Z) * vox_controller.voxel_grid.voxel_size;
			}
		}
	}
}
