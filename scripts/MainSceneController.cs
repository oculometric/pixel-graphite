using Godot;
using System;
using System.Collections.Generic;

public partial class MainSceneController : Node3D
{
	[Export] public EditingUIController ui_controller;

	[Export] public EditController[] editors { get; private set; }
	public VoxelEditController voxel_editor { get; private set; }

	public int editing_mode { get; private set; } = 0;
	private bool editor_input_enabled = true;
	private bool is_mode_selecting = false;

	public override void _Ready()
	{
		ui_controller.ConfigureEditModalUI(this);
		SetEditingMode(0);
		foreach (EditController ec in editors)
		{
			if (ec is VoxelEditController)
				voxel_editor = ec as VoxelEditController;
		}
	}

	public void ToggleAllEditorInput(bool enabled)
	{
		editor_input_enabled = enabled;
		int i = 0;
		foreach (EditController ec in editors)
		{
			ec.SetEditingEnabled(enabled ? i == editing_mode : false);
			ec.SetProcessUnhandledInput(enabled ? i == editing_mode : false);
			i++;
		}
	}

	private void SetEditingMode(int new_editing_mode)
	{
		int nem = new_editing_mode;
		if (nem >= editors.Length)
			nem = editors.Length - 1;
		
		is_mode_selecting = false;
		if (nem >= 0)
			editing_mode = new_editing_mode;
		else is_mode_selecting = true;

		if (nem >= 0)
			ui_controller.SetEditingMode(editing_mode, editors[nem]);
		ui_controller.SetModalVisible(is_mode_selecting);

		int i = 0;
		foreach (EditController ec in editors)
		{
			ec.SetEditingEnabled(nem == i);
			ec.SetProcessUnhandledInput(nem == i);
			i++;
		}
	}

    public void TakeScreenshot()
    {
        GD.Print("screenshot");
        DirAccess screenshot_dir = DirAccess.Open(".");
        screenshot_dir.MakeDir("screenshots");
        screenshot_dir.ChangeDir("screenshots");
        int image_number = 0;
        List<string> files = new List<string>(screenshot_dir.GetFiles());
        while (files.Contains(string.Format("pixel_graphite_{0:D4}.png", image_number)))
            image_number++;
        string image_name = screenshot_dir.GetCurrentDir() + string.Format("/pixel_graphite_{0:D4}.png", image_number);
        Image image_data = GetViewport().GetTexture().GetImage();
        image_data.Resize(3072, 3072, Image.Interpolation.Nearest);
        image_data.SavePng(image_name);
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
					case Key.P: TakeScreenshot(); break;
					case Key.Tab:
						if (!is_mode_selecting)
							SetEditingMode(-1);
						else
							SetEditingMode(editing_mode);
						break;
					case Key.Key1: if (is_mode_selecting) SetEditingMode(0); break;
					case Key.Key2: if (is_mode_selecting) SetEditingMode(1); break;
					case Key.Key3: if (is_mode_selecting) SetEditingMode(2); break;
					case Key.Key4: if (is_mode_selecting) SetEditingMode(3); break;
					case Key.Key5: if (is_mode_selecting) SetEditingMode(4); break;
					case Key.Key6: if (is_mode_selecting) SetEditingMode(5); break;
					case Key.Key7: if (is_mode_selecting) SetEditingMode(6); break;
					case Key.Key8: if (is_mode_selecting) SetEditingMode(7); break;
					case Key.Key9: if (is_mode_selecting) SetEditingMode(8); break;
					case Key.Quoteleft: GetViewport().DebugDraw = (Viewport.DebugDrawEnum)((int)(GetViewport().DebugDraw + 1) % 6); break;
					default:
						ui_controller._GuiInput(@event);
						break;
				}
			}
		}
		else if (@event is InputEventMouse && !editor_input_enabled)
			ui_controller._GuiInput(@event);
	}
}
