using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class CameraController : Node3D
{
	[Export] Node3D camera_spin;
	[Export] Node3D camera_pitch;
	[Export] Camera3D camera;

	[Export] MainSceneController scene_controller;

	[Export] float max_pan_velocity = 16.0f;

	Vector2 pan_velocity = Vector2.Zero;
	Vector2 mouse_delta = Vector2.Zero;
	Vector3 lerp_target;
	bool is_free_look = true;

	float angle_target;

	public override void _Ready()
	{
		lerp_target = GlobalPosition;
		angle_target = camera_pitch.Rotation.X;
	}

	public override void _PhysicsProcess(double delta)
	{
		GetWindow().Title = "pixel graphite (" + Engine.GetFramesPerSecond().ToString("000.0") + " fps)";

		ProcessCameraInput((float)delta);
		GlobalPosition = GlobalPosition + Interp(GlobalPosition, lerp_target, (float)delta);
		if (!is_free_look)
			camera_pitch.RotateX(Interp(camera_pitch.Rotation.X, angle_target, (float)delta));
	}

	private float InterpLength(float length, float delta)
	{
		return Mathf.Clamp(((10.0f * Mathf.Log(length + 1.0f)) + 0.1f) * delta * 1.0f, 0.0f, length);
	}

	private float Interp(float current, float target, float delta)
	{
		float towards = target - current;
		float length = Mathf.Abs(towards);
		if (length < 0.01f)
			return towards;
		float direction = Mathf.Sign(towards);
		return direction * InterpLength(length, delta);
	}

	private Vector3 Interp(Vector3 current, Vector3 target, float delta)
	{
		Vector3 towards = target - current;
		float length = towards.Length();
		if (length < 0.01f)
			return towards;
		Vector3 direction = towards / length;
		return direction * InterpLength(length, delta);
	}

	private void ProcessCameraInput(float delta)
	{
		camera_spin.GlobalPosition = GlobalPosition;

		camera_spin.RotateY(pan_velocity.X * (float)delta);
		if (is_free_look)
			camera_pitch.RotateX(pan_velocity.Y * (float)delta);
		else
			camera_pitch.GlobalTranslate(Vector3.Up * -pan_velocity.Y * 5.0f * (float)delta);

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
		else if (@event is InputEventMouseButton)
		{
			InputEventMouseButton button = @event as InputEventMouseButton;
			if (button.IsPressed() && button.ButtonIndex == MouseButton.Right)
			{
				Vector3I cell = scene_controller.voxel_editor.GetHighlightedCell(true);
				GlobalPosition = camera_pitch.GlobalPosition;
				camera_pitch.GlobalPosition = GlobalPosition;
				lerp_target = new Vector3(cell.X, cell.Y, cell.Z) * scene_controller.voxel_editor.voxel_grid.voxel_size;
			}
		}
		else if (@event is InputEventKey)
		{
			InputEventKey key = @event as InputEventKey;
			if (key.IsPressed())
			{
				switch (key.Keycode)
				{
					case Key.Z: if (!is_free_look) angle_target = -angle_target; break;
					case Key.F: 
						is_free_look = !is_free_look;
						if (is_free_look)
						{
							GlobalPosition = camera_pitch.GlobalPosition;
							camera_pitch.GlobalPosition = GlobalPosition;
							lerp_target = GlobalPosition;
						}
						break;
				}
			}
		}
	}
}
