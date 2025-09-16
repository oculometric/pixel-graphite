using Godot;
using Godot.Collections;

public partial class VoxelEditController : EditController
{
	private Vector2 mouse_delta;
	private Voxel current_cell_type;
	private byte cell_type_index = 1;
	private bool erase_mode = false;
	public MeshInstance3D outline_object;
	private Vector3 ghost_object_target_pos;
	private Vector2 pre_drag_mouse_pos;
	
	[Export] public SaveManager save_manager { get; private set; }
	[Export] public VoxelGrid voxel_grid { get; private set; }
	[Export] private Mesh outline_mesh;
 
    public override async void _Ready()
    {
		current_cell_type = new Voxel(cell_type_index, 0);
		scene_controller.ui_controller.ConfigureVoxelUI(this);
        scene_controller.ui_controller.UpdateVoxelUI(cell_type_index, erase_mode, current_cell_type.orientation);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		UpdateOutlineMesh();
		if (FileAccess.FileExists("res://startup.dat"))
			voxel_grid.Load("res://startup.dat");
		else
			GD.Print("startup file missing!");

    }

	public override void SetEditingEnabled(bool enabled)
	{
		if (enabled)
		{
			if (outline_object != null) outline_object.Visible = scene_controller.ui_controller.Visible;
		}
		else
		{
			outline_object.Visible = false;
		}
	}

	private void UpdateOutlineMesh()
	{
		Vector3I cell = GetHighlightedCell(erase_mode);
		if (outline_object == null)
		{
            outline_object = new MeshInstance3D();
			outline_object.MaterialOverride = outline_mesh.SurfaceGetMaterial(0);
			outline_object.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            GetParent().AddChild(outline_object);
			outline_object.GlobalPosition = new Vector3(cell.X, cell.Y, cell.Z) * voxel_grid.voxel_size;
        }

		outline_object.Mesh = erase_mode ? outline_mesh : voxel_grid.voxel_types[current_cell_type.id].geometry;
		outline_object.RotationDegrees = new Vector3(0, 90.0f * current_cell_type.orientation, 0);
		outline_object.Scale = new Vector3(1, (current_cell_type.orientation & 0b100) > 0 ? -1 : 1, 1);
		outline_object.Visible = scene_controller.ui_controller.editors_should_show_gizmos;

		ghost_object_target_pos = new Vector3(cell.X, cell.Y, cell.Z) * voxel_grid.voxel_size;
	}

    private void PerformInteraction()
	{
		// set that part of the block grid to the assigned block type
		voxel_grid.SetCellValue(GetHighlightedCell(erase_mode), erase_mode ? new Voxel(0, 0) : current_cell_type);
		voxel_grid.Rebuild();
		save_manager.SetUnsavedFlag();
		UpdateOutlineMesh();
	}

	public Vector3I GetHighlightedCell(bool inside)
	{
        // raycast to find geometry intersection
        PhysicsRayQueryParameters3D ray_params = new PhysicsRayQueryParameters3D();
        ray_params.From = GetViewport().GetCamera3D().GlobalPosition;
		Vector2 pos = GetViewport().GetMousePosition();
        Vector3 ray_direction = GetViewport().GetCamera3D().ProjectRayNormal(pos);
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
		if (voxel_grid == null)
			return Vector3I.Zero;

        Vector3I highlighted_cell = (Vector3I)(hit_cell / voxel_grid.voxel_size).Round();

		return highlighted_cell;
    }

	bool is_dragging = false;
	
    public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton)
		{
			InputEventMouseButton button = @event as InputEventMouseButton;
			if (button.Pressed == true)
				mouse_delta = Vector2.Zero;
			else
			{
				if (is_dragging)
				{
					Input.MouseMode = Input.MouseModeEnum.Visible;
					Input.WarpMouse(pre_drag_mouse_pos);
					is_dragging = false;
				}
				else if (button.ButtonIndex == MouseButton.Left)
				{
					PerformInteraction();
				}
			}
		}
		else if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion motion = @event as InputEventMouseMotion;
			if (!is_dragging)
				pre_drag_mouse_pos = GetWindow().GetMousePosition();
			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				mouse_delta += motion.Relative;
				if (mouse_delta.Length() > 10.0f && !is_dragging)
				{
					is_dragging = true;
					Input.MouseMode = Input.MouseModeEnum.Captured;
				}
			}
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
					case Key.Key6: cell_type_index = 6; erase_mode = false; break;
					case Key.Key7: cell_type_index = 7; erase_mode = false; break;
					case Key.Key8: cell_type_index = 8; erase_mode = false; break;
					case Key.Key9: cell_type_index = 9; erase_mode = false; break;
					case Key.D: current_cell_type.orientation = (byte)((((current_cell_type.orientation & 0b11) + 3) % 4) | (current_cell_type.orientation & 0b100)); break;
					case Key.A: current_cell_type.orientation = (byte)((((current_cell_type.orientation & 0b11) + 1) % 4) | (current_cell_type.orientation & 0b100)); break;
					case Key.F: current_cell_type.orientation = (byte)(current_cell_type.orientation ^ 0b100); break;
					case Key.X: erase_mode = !erase_mode; break;
                }
                current_cell_type = new Voxel(cell_type_index, current_cell_type.orientation);
                scene_controller.ui_controller.UpdateVoxelUI(cell_type_index, erase_mode, current_cell_type.orientation);
				UpdateOutlineMesh();
            }
		}
	}

    public override void _Process(double delta)
    {
		Vector3 to_target = ghost_object_target_pos - outline_object.GlobalPosition;
		float dist = to_target.Length();
		if (dist < 0.01f)
		{
			outline_object.GlobalPosition = ghost_object_target_pos;
			return;
		}
		Vector3 normalised = to_target / dist;
		float result_dist = Mathf.Clamp((dist * Mathf.Pow(0.0005f, (float)delta)) - (0.2f * (float)delta), 0.0f, 10000.0f);
        outline_object.GlobalPosition = ghost_object_target_pos - (normalised * result_dist);
    }
}
