using Godot;
using System;
using System.Threading.Tasks;

public partial class EditingUIController : Control
{
	[Export] private Label editing_mode_label;
	[Export] private GridContainer mode_modal;
	[Export] private Label top_label;

	[Export] private FileDialog file_dialog;
	[Export] private ConfirmationDialog confirm_discard_dialog;
	[Export] private AcceptDialog accept_dialog;

	[Export] private TabContainer help_panel;

    [Export] private VBoxContainer voxel_palette;
	private MainSceneController scene_controller;

	private bool is_exporting = false;
	public bool editors_should_show_gizmos { get; private set; } = true;

	public void ConfigureVoxelUI(VoxelEditController vec)
	{
		Node template = voxel_palette.GetChild(0);
		int i = 0;
		foreach (VoxelType vt in vec.voxel_grid.voxel_types)
		{
			if (i == 0)
			{
				i++;
				continue;
			}
			Node copy = template.Duplicate();
			copy.GetChild(0).GetChild<Label>(1).Text = "(" + i + ") " + vt.name;
			copy.GetChild(0).GetChild<TextureRect>(0).Texture = vt.ui_texture;
			template.GetParent().AddChild(copy);
			i++;
		}

		file_dialog.FileSelected += (string path) =>
		{
			if (is_exporting)
				vec.voxel_grid.Export(path);
			else if (file_dialog.FileMode == FileDialog.FileModeEnum.SaveFile)
				save_callback(path);
			else if (file_dialog.FileMode == FileDialog.FileModeEnum.OpenFile)
				load_callback(path);
		};
	}

	public void ConfigureEditModalUI(MainSceneController msc)
	{
		scene_controller = msc;
		Node template = mode_modal.GetChild(0);
		int i = 0;
		foreach (EditController ec in msc.editors)
		{
			template.GetChild(0).GetChild<TextureRect>(0).Texture = ec.ui_icon;
			template.GetChild(0).GetChild<Label>(1).Text = "(" + (i + 1) + ") " + ec.ui_name;
			Node copy = template.Duplicate();
			mode_modal.AddChild(copy);
			template = copy;
			i++;
		}
		template.QueueFree();
	}

	public override void _Ready()
	{
		confirm_discard_dialog.Confirmed += () => { confirm_callback(); };

		GetWindow().SizeChanged += () =>
		{
			(GetViewport() as SubViewport).Size = GetWindow().Size;
		};
		SetModalVisible(false);
		help_panel.Visible = false;
	}

	public Action<string> save_callback;
	public Action<string> load_callback;

	public void UpdateVoxelUI(int selected_voxel_type, bool erase_mode, byte orientation)
	{
		StyleBoxFlat style_box = new StyleBoxFlat();
		style_box.AntiAliasing = false;
		style_box.BorderWidthBottom = 2;
		style_box.BorderWidthTop = 2;
		style_box.BorderWidthLeft = 2;
		style_box.BorderWidthRight = 2;
		style_box.DrawCenter = false;
		style_box.BorderColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		for (int i = 0; i < voxel_palette.GetChildCount(); i++)
		{
			if ((i == 0 && erase_mode) || (i == selected_voxel_type && !erase_mode))
			{
				StyleBoxFlat tmp = style_box.Duplicate() as StyleBoxFlat;
				tmp.BorderColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
				voxel_palette.GetChild<PanelContainer>(i).AddThemeStyleboxOverride("panel", tmp);
			}
			else
				voxel_palette.GetChild<PanelContainer>(i).AddThemeStyleboxOverride("panel", style_box);
		}
	}

	public void SetModalVisible(bool visible)
	{
		if (visible)
		{
			Visible = true;
			ShowPanel(0, true);
		}
		voxel_palette.GetParent<Control>().Visible = !visible;
		top_label.Visible = !visible;
		mode_modal.Visible = visible;
		editors_should_show_gizmos = !visible;
	}

	public void SetEditingMode(int mode, EditController editor)
	{
		editing_mode_label.Text = "(TAB) editing: " + editor.ui_name;
		top_label.Text = editor.ui_controls;

		StyleBoxFlat style_box = new StyleBoxFlat();
		style_box.AntiAliasing = false;
		style_box.BorderWidthBottom = 2;
		style_box.BorderWidthTop = 2;
		style_box.BorderWidthLeft = 2;
		style_box.BorderWidthRight = 2;
		style_box.DrawCenter = false;
		style_box.BorderColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		for (int i = 0; i < mode_modal.GetChildCount(); i++)
		{
			if (i == mode)
			{
				StyleBoxFlat tmp = style_box.Duplicate() as StyleBoxFlat;
				tmp.BorderColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
				mode_modal.GetChild<PanelContainer>(i).AddThemeStyleboxOverride("panel", tmp);
			}
			else
				mode_modal.GetChild<PanelContainer>(i).AddThemeStyleboxOverride("panel", style_box);
		}

		if (editor.ui_name == "voxels")
			voxel_palette.Visible = true;
		else
			voxel_palette.Visible = false;
	}

	public void ShowSaveDialog(string file_name)
	{
		file_dialog.Title = "save";
		file_dialog.OkButtonText = "save";
		file_dialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		is_exporting = false;
		file_dialog.Filters = ["*.dat"];
		file_dialog.CurrentPath = file_name;
        file_dialog.Show();
	}

	public void ShowLoadDialog(string file_name)
	{
		file_dialog.Title = "load";
		file_dialog.OkButtonText = "load";
        file_dialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		is_exporting = false;
		file_dialog.Filters = ["*.dat"];
		file_dialog.CurrentPath = file_name;
		file_dialog.Show();
    }

	public void ShowExportDialog()
	{
		file_dialog.Title = "export";
		file_dialog.OkButtonText = "export";
		file_dialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		is_exporting = true;
		file_dialog.Filters = ["*.obj"];
		file_dialog.CurrentFile = "";
		file_dialog.Show();
	}

	private Action confirm_callback = null;
	public void ShowConfirmDialog(string title, string body, string ok, string cancel, Action callback)
	{
		confirm_discard_dialog.Title = title;
		confirm_discard_dialog.DialogText = body;
		confirm_discard_dialog.OkButtonText = ok;
		confirm_discard_dialog.CancelButtonText = cancel;
		confirm_callback = callback;
		confirm_discard_dialog.Show();
	}

	public void ShowErrorDialog(string body)
	{
		accept_dialog.DialogText = body;
		accept_dialog.Show();
	}

	public void ShowPanel(int index, bool hide = false)
	{
		bool new_visible = index == help_panel.CurrentTab ? !help_panel.Visible : true;
		help_panel.CurrentTab = index;
		if (mode_modal.Visible || hide)
			new_visible = false;
		help_panel.Visible = new_visible;
		if (mode_modal.Visible)
			return;
		voxel_palette.GetParent<Control>().Visible = !new_visible;
		if (new_visible)
			Visible = true;
		editors_should_show_gizmos = !new_visible;
		scene_controller.ToggleAllEditorInput(!new_visible);
	}
	
	public override void _GuiInput(InputEvent @event)
	{
		GD.Print(@event);
		if (@event is InputEventKey)
		{
			InputEventKey key = @event as InputEventKey;
			if (key.IsPressed())
			{
				switch (key.Keycode)
				{
					case Key.H:
						if (mode_modal.Visible)
							break;
						if (key.CtrlPressed)
						{
							ShowPanel(0);
							break;
						}
						Visible = !Visible;
						break;
					case Key.U: ShowExportDialog(); break;
					case Key.F1:
						ShowPanel(0);
						break;
					case Key.Escape:
						ShowPanel(0, true);
						break;
					case Key.K:
						if (key.CtrlPressed)
							ShowPanel(1);
						break;
				}
			}
		}
	}

}
