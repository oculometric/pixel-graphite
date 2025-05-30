using Godot;

[GlobalClass]
public partial class VoxelType : Resource
{
	[Export] public string name = "empty";
	[Export] public Mesh geometry;
	[Export] public Texture2D ui_texture;

	public VoxelType()
	{
	}

	public VoxelType(string n, Mesh g)
	{
		name = n;
		geometry = g;
	}
}
