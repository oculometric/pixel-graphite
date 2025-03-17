using Godot;
using System;

public partial class UpdateUIRender : ColorRect
{
	[Export]
	SubViewport hud_viewport;

	public override void _Process(double delta)
	{
		(Material as ShaderMaterial).SetShaderParameter("ui_texture", hud_viewport.GetTexture());
		hud_viewport.Size = (Vector2I)GetViewportRect().Size / 2;
	}
}
