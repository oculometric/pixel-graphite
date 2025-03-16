using Godot;
using Godot.Collections;
using System;

public partial class VoxelEditController : Node3D
{
	private Vector2 mouse_delta;
	private Voxel current_cell_type;
	
	[Export] public VoxelGrid voxel_grid { get; private set; }

    public override void _Ready()
    {
		current_cell_type = new Voxel(voxel_grid.voxel_types[1], 0);
    }

    private void PerformInteraction()
	{
		// raycast to find geometry intersection
		PhysicsRayQueryParameters3D ray_params = new PhysicsRayQueryParameters3D();
		ray_params.From = GetViewport().GetCamera3D().GlobalPosition;
		Vector3 ray_direction = GetViewport().GetCamera3D().ProjectRayNormal(GetViewport().GetMousePosition());
		ray_params.To = ray_params.From + (ray_direction * 2000.0f);
		Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(ray_params);
		// if none, check use intersection with world floor
		Vector3 hit_cell = Vector3.Zero;
		if (result.Count == 0)
		{
			float t = -ray_params.From.Y / ray_direction.Y;
			hit_cell = ray_params.From + (ray_direction * t);
		}
		else
			hit_cell = ((Vector3)result["position"]) + ((Vector3)result["normal"] * voxel_grid.voxel_size * 0.5f);

		// calculate highlighted cell in grid
		Vector3I highlighted_cell = (Vector3I)(hit_cell / voxel_grid.voxel_size).Round();

		// set that part of the block grid to the assigned block type
		voxel_grid.SetCellValue(highlighted_cell, current_cell_type);
		voxel_grid.Rebuild();
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton)
		{
			InputEventMouseButton button = @event as InputEventMouseButton;
			if (button.Pressed == true)
				mouse_delta = Vector2.Zero;
			else if (mouse_delta.Length() < 10.0f)
				PerformInteraction();
		}
		else if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion motion = @event as InputEventMouseMotion;
			if (Input.IsMouseButtonPressed(MouseButton.Left))
				mouse_delta += motion.Relative;
		}
	}
}
