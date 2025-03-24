using Godot;
using System;
using System.Threading.Tasks;

public partial class EditingUIController : Control
{
	[Export] private Label editing_mode_label;
	[Export] private HBoxContainer mode_modal;
	[Export] private Label top_label;

	[Export] private FileDialog file_dialog;

	[Export] private VBoxContainer voxel_type_container;

	private string[] mode_names = [ "voxels", "object", "sand & grass", "lighting", "none" ];
	private string[] mode_controls = [ "(A) << rotate >> (D)\n(F) flip vertical", "...", "...", "(A) << rotate >> (D)    (W/S) rotate up/down    (SHIFT) disable snap\n(Q) toggle sun    (E) toggle ambient    (R) toggle shadows", "..." ];

	public void ConfigureUI(VoxelEditController vec)
	{
		Node template = voxel_type_container.GetChild(0);
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
			if (file_dialog.FileMode == FileDialog.FileModeEnum.SaveFile)
				vec.Save(path);
			else if (file_dialog.FileMode == FileDialog.FileModeEnum.OpenFile)
				vec.Load(path);
		};

		ToggleModeModal(false, 0);
	}

	public void UpdateVoxelUI(int selected_voxel_type, bool erase_mode, byte orientation)
	{
		StyleBoxFlat style_box = new StyleBoxFlat();
		style_box.AntiAliasing = false;
		style_box.BorderWidthBottom = 1;
		style_box.BorderWidthTop = 1;
		style_box.BorderWidthLeft = 1;
		style_box.BorderWidthRight = 1;
		style_box.DrawCenter = false;
		style_box.BorderColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		for (int i = 0; i < voxel_type_container.GetChildCount(); i++)
		{
			if ((i == 0 && erase_mode) || (i == selected_voxel_type && !erase_mode))
			{
				StyleBoxFlat tmp = style_box.Duplicate() as StyleBoxFlat;
				tmp.BorderColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
				voxel_type_container.GetChild<PanelContainer>(i).AddThemeStyleboxOverride("panel", tmp);
			}
			else
				voxel_type_container.GetChild<PanelContainer>(i).AddThemeStyleboxOverride("panel", style_box);
		}
	}

	public void ToggleModeModal(bool visible, int current_mode)
	{
		voxel_type_container.Visible = !visible;
		top_label.Visible = !visible;

		mode_modal.Visible = visible;
		if (visible)
		{
			StyleBoxFlat style_box = new StyleBoxFlat();
			style_box.AntiAliasing = false;
			style_box.BorderWidthBottom = 1;
			style_box.BorderWidthTop = 1;
			style_box.BorderWidthLeft = 1;
			style_box.BorderWidthRight = 1;
			style_box.DrawCenter = false;
			style_box.BorderColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
			for (int i = 0; i < 5; i++)
			{
				if (i == current_mode)
				{
					StyleBoxFlat tmp = style_box.Duplicate() as StyleBoxFlat;
					tmp.BorderColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
					mode_modal.GetChild<PanelContainer>(i).AddThemeStyleboxOverride("panel", tmp);
				}
				else
					mode_modal.GetChild<PanelContainer>(i).AddThemeStyleboxOverride("panel", style_box);
			}
		}
	}

	public void SetEditingMode(int mode)
	{
		// TODO: hide/show different bits of the UI
		editing_mode_label.Text = "editing: " + mode_names[mode];
		top_label.Text = mode_controls[mode];

		switch (mode)
		{
			case 0:
				voxel_type_container.Visible = true;
				break;
			case 1:
				voxel_type_container.Visible = false;
				break;
			case 2:
				voxel_type_container.Visible = false;
				break;
			case 3:
				voxel_type_container.Visible = false;
				break;
			case 4:
				voxel_type_container.Visible = false;
				break;
		}
	}

	public void ShowSaveDialog()
	{
		file_dialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		file_dialog.Show();
	}

	public void ShowLoadDialog()
	{
		file_dialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		file_dialog.Show();
	}
}
