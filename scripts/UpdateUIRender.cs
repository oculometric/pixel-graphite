using Godot;
using System;

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
}
