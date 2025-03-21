using Godot;
using System;
using System.Collections.Generic;

public partial class UpdateUIRender : ColorRect
{
	[Export]
	SubViewport hud_viewport;
	[Export]
	SubViewport game_viewport;

	public override void _Process(double delta)
	{
		(Material as ShaderMaterial).SetShaderParameter("game_texture", game_viewport.GetTexture());
		(Material as ShaderMaterial).SetShaderParameter("ui_texture", hud_viewport.GetTexture());
		hud_viewport.Size = (Vector2I)GetViewportRect().Size / 2;
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
}
