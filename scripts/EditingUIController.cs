using Godot;
using System;

public partial class EditingUIController : Control
{
	[Export] private VBoxContainer voxel_type_container;

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
			template.GetParent().AddChild(copy);
			i++;
		}
	}

	public void UpdateUI(int selected_voxel_type, bool erase_mode, byte orientation)
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
}
