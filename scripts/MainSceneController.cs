using Godot;
using System;

public partial class MainSceneController : Node3D
{
	[Export] public EditingUIController ui_controller;
	[Export] public UpdateUIRender render_controller;

	[Export] public VoxelEditController voxel_editor;

	private int editing_mode = 0;   // editing modes:
									// 0 - voxel editing
									// 1 - object editing
									// 2 - grass editing
									// 3 - light editing
									// 4 - no editing
	private bool is_modal = false;

	private void SetEditingMode(int new_editing_mode)
	{
		is_modal = false;
		ui_controller.ToggleModeModal(is_modal, new_editing_mode);

		switch (editing_mode)
		{
			case 0:
				voxel_editor.outline_object.Visible = false;
				voxel_editor.SetProcessUnhandledInput(false);
				break;
			case 1:
				// TODO: disable object editor
				break;
			case 2:
				// TODO: disable grass editor
				break;
			case 3:
				// TODO: disable light editor
				break;
		}

		switch (new_editing_mode)
		{
			case 0:
				voxel_editor.outline_object.Visible = ui_controller.Visible;
				voxel_editor.SetProcessUnhandledInput(true);
				break;
			case 1:
				// TODO: enable object editor
				break;
			case 2:
				// TODO: enable grass editor
				break;
			case 3:
				// TODO: enable light editor
				break;
		}

		editing_mode = new_editing_mode;
		ui_controller.SetEditingMode(editing_mode);
	}

	private void SetModal(bool modal)
	{
		is_modal = modal;
		if (is_modal)
		{
			ui_controller.Visible = true;
			voxel_editor.outline_object.Visible = ui_controller.Visible && editing_mode == 0;
		}

		voxel_editor.outline_object.Visible = false;
		voxel_editor.SetProcessUnhandledInput(false);
		// TODO: disable input to all other editors during modal

		ui_controller.ToggleModeModal(is_modal, editing_mode);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey)
		{
			InputEventKey key = @event as InputEventKey;
			if (key.IsPressed())
			{
				switch (key.Keycode)
				{
					case Key.P: render_controller.TakeScreenshot(); break;
					case Key.H:
						if (is_modal) break;
						ui_controller.Visible = !ui_controller.Visible;
						voxel_editor.outline_object.Visible = ui_controller.Visible && editing_mode == 0;
						break;
					case Key.O: ui_controller.ShowSaveDialog(); break;
					case Key.I: ui_controller.ShowLoadDialog(); break;
					case Key.Tab:
						if (!is_modal) SetModal(true);
						else SetEditingMode(editing_mode);
						break;
					case Key.Key1: if (is_modal) SetEditingMode(0); break;
					case Key.Key2: if (is_modal) SetEditingMode(1); break;
					case Key.Key3: if (is_modal) SetEditingMode(2); break;
					case Key.Key4: if (is_modal) SetEditingMode(3); break;
					case Key.Key5: if (is_modal) SetEditingMode(4); break;
				}
			}
		}
	}
}
