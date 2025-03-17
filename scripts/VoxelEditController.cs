using Godot;
using Godot.Collections;
using System;

public partial class VoxelEditController : Node3D
{
	private Vector2 mouse_delta;
	private Voxel current_cell_type;
	private int cell_type_index = 1;
	private bool erase_mode = false;
	private MeshInstance3D outline_object;
	
	[Export] public VoxelGrid voxel_grid { get; private set; }
	[Export] public EditingUIController ui_controller;
	[Export] private Mesh outline_mesh;
 
    public override void _Ready()
    {
		current_cell_type = new Voxel(voxel_grid.voxel_types[cell_type_index], 0);
		ui_controller.ConfigureUI(this);
        ui_controller.UpdateUI(cell_type_index, erase_mode, current_cell_type.orientation);
    }

	private void UpdateOutlineMesh()
	{
		if (outline_object == null)
		{
            outline_object = new MeshInstance3D();
			outline_object.MaterialOverride = outline_mesh.SurfaceGetMaterial(0);
            GetParent().AddChild(outline_object);
        }

		outline_object.Mesh = erase_mode ? outline_mesh : current_cell_type.type.geometry;
		outline_object.RotationDegrees = new Vector3(0, 90.0f * current_cell_type.orientation, 0);

		Vector3I cell = GetHighlightedCell(erase_mode);
        outline_object.GlobalPosition = new Vector3(cell.X, cell.Y, cell.Z) * voxel_grid.voxel_size;
	}

    private void PerformInteraction()
	{
		// set that part of the block grid to the assigned block type
		voxel_grid.SetCellValue(GetHighlightedCell(erase_mode), erase_mode ? new Voxel(voxel_grid.voxel_types[0], 0) : current_cell_type);
		voxel_grid.Rebuild();
	}

	public Vector3I GetHighlightedCell(bool inside)
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
            hit_cell = ((Vector3)result["position"]) + ((Vector3)result["normal"] * voxel_grid.voxel_size * (inside ? -0.5f : 0.5f));

        // calculate highlighted cell in grid
        Vector3I highlighted_cell = (Vector3I)(hit_cell / voxel_grid.voxel_size).Round();

		return highlighted_cell;
    }
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton)
		{
			InputEventMouseButton button = @event as InputEventMouseButton;
			if (button.Pressed == true)
				mouse_delta = Vector2.Zero;
			else if (mouse_delta.Length() < 10.0f && button.ButtonIndex == MouseButton.Left)
				PerformInteraction();
		}
		else if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion motion = @event as InputEventMouseMotion;
			if (Input.IsMouseButtonPressed(MouseButton.Left))
				mouse_delta += motion.Relative;
			else
				UpdateOutlineMesh();
		}
		else if (@event is InputEventKey)
		{
			InputEventKey key = @event as InputEventKey;
			if (key.IsPressed())
			{
				switch (key.Keycode)
				{
					case Key.Key1: cell_type_index = 1; erase_mode = false; break;
					case Key.Key2: cell_type_index = 2; erase_mode = false; break;
					case Key.Key3: cell_type_index = 3; erase_mode = false; break;
					case Key.Key4: cell_type_index = 4; erase_mode = false; break;
					case Key.Key5: cell_type_index = 5; erase_mode = false; break;
					case Key.D: current_cell_type.orientation = (byte)((current_cell_type.orientation + 3) % 4); break;
					case Key.A: current_cell_type.orientation = (byte)((current_cell_type.orientation + 1) % 4); break;
					case Key.X: erase_mode = !erase_mode; break;
                }
                current_cell_type = new Voxel(voxel_grid.voxel_types[cell_type_index], current_cell_type.orientation);
                ui_controller.UpdateUI(cell_type_index, erase_mode, current_cell_type.orientation);
				UpdateOutlineMesh();
            }
		}
	}
}
