using Godot;

public partial class ViewportTextureGrab : MeshInstance3D
{
    [Export] Godot.Collections.Array<SubViewport> viewports;
    [Export] Godot.Collections.Array<string> shader_parameters;

    public override void _Process(double delta)
    {
        for (int i = 0; i < viewports.Count; i++)
        {
            if (i >= shader_parameters.Count)
                break;
            if (viewports[i] != null) (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter(shader_parameters[i], viewports[i].GetTexture());
        }
    }
}