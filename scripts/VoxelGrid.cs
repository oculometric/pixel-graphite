using Godot;
using System;
using System.Collections.Generic;

public enum Voxel
{
	EMPTY =			0b00000000,
	SOLID =			0b00000001,
	NARROW_ARCH =   0b00000100, // last two bits reserved for orientation
	STAIR =			0b00001000, // last three bits reserved for rotation
	ARCH  =			0b00010000, // ^
	SLOPE =			0b00100000, // ^
}

public partial class VoxelGrid : MeshInstance3D
{
	private List<List<List<Voxel>>> voxel_map;
	private Vector3I voxel_origin = new Vector3I(0, 0, 0);

	[Export] uint initial_size = 7;

	public override void _Ready()
	{
		voxel_map = new List<List<List<Voxel>>>();
		for (int i = 0; i < initial_size; i++)
		{
			List<List<Voxel>> area = new List<List<Voxel>>();

			for (int j = 0; j < initial_size; j++)
			{
				List<Voxel> row = new List<Voxel>();

				for (int k = 0; k < initial_size; k++)
				{
					row.Add(Voxel.EMPTY);
				}

				area.Add(row);
			}

			voxel_map.Add(area);
		}

		int o = (((int)initial_size - 1) / 2);
		voxel_origin = new Vector3I(o, o, o);
		GD.Print("origin: " + voxel_origin + " dimensions: " + voxel_map[0][0].Count + "," + voxel_map[0].Count + "," + voxel_map.Count);

		SetCellValue(voxel_origin, Voxel.SOLID);
	}

	public void SetCellValue(Vector3I position, Voxel type)
	{
		// calculate position in the array based on position and voxel_origin
		int array_x_pos = position.X + voxel_origin.X;
		int array_y_pos = position.Y + voxel_origin.Y;
		int array_z_pos = position.Z + voxel_origin.Z;

		GD.Print("set voxel: " + position + " in arrays: " + array_x_pos + "," + array_y_pos + "," + array_z_pos);

		// expand if necessary, in each of the three dimensions one after another
		if (array_x_pos < 0)
		{
			ExtendInDirection(1, -array_x_pos);
			array_x_pos = position.X + voxel_origin.X;
		}
		if (array_x_pos >= voxel_map[0][0].Count)
			ExtendInDirection(0, (array_x_pos - voxel_map[0][0].Count) + 1);
		if (array_y_pos < 0)
		{
			ExtendInDirection(3, -array_y_pos);
			array_y_pos = position.Y + voxel_origin.Y;
		}
		if (array_y_pos >= voxel_map[0].Count)
			ExtendInDirection(1, (array_y_pos - voxel_map[0].Count) + 1);
		if (array_z_pos < 0)
		{
			ExtendInDirection(5, -array_z_pos);
			array_z_pos = position.Z + voxel_origin.Z;
		}
		if (array_z_pos >= voxel_map.Count)
			ExtendInDirection(4, (array_z_pos - voxel_map.Count) + 1);

		// set value
		voxel_map[array_z_pos][array_y_pos][array_x_pos] = type;
	}
	
	public void ExtendInDirection(int dir, int amount)
	{
		// add new arrays/voxels at the beginning/end of existing arrays (depending on the direction)
		switch (dir)
		{
			case 0: // +x, innermost arrays
			case 1: // -x
				for (int i = 0; i < voxel_map.Count; i++)
				{
					for (int j = 0; j < voxel_map[i].Count; j++)
					{
						for (int _ = 0; _ < amount; _++)
						{
							if (dir == 0)
								voxel_map[i][j].Add(Voxel.EMPTY);
							else if (dir == 1)
								voxel_map[i][j].Insert(0, Voxel.EMPTY);
						}
					}
				}
				break;
			case 2: // +y, middle arrays
			case 3: // -y
				for (int i = 0; i < voxel_map.Count; i++)
				{
					for (int _ = 0; _ < amount; _++)
					{
						List<Voxel> arr = new List<Voxel>();
						for (int j = 0; j < voxel_map[i].Count; j++)
							arr.Add(Voxel.EMPTY);
						if (dir == 2)
							voxel_map[i].Add(arr);
						else if (dir == 3)
							voxel_map[i].Insert(0, arr);
					}
				}
				break;
			case 4: // +z, outermost arrays
			case 5: // -z
				for (int _ = 0; _ < amount; _++)
				{
					List<List<Voxel>> arr = new List<List<Voxel>>();
					for (int j = 0; j < voxel_map[0].Count; j++)
					{
						List<Voxel> arr2 = new List<Voxel>();
						for (int k = 0; k < voxel_map[0][j].Count; k++)
							arr2.Add(Voxel.EMPTY);
						arr.Add(arr2);
					}
					if (dir == 4)
						voxel_map.Add(arr);
					else if (dir == 5)
						voxel_map.Insert(0, arr);
				}
				break;
		}

		// update voxel origin if adding at the beginning
		Vector3I offset = Vector3I.Zero;
		switch (dir)
		{
			//case 0: offset.X = 0; break;
			case 1: offset.X = amount; break;
			//case 2: offset.Y = 0; break;
			case 3: offset.Y = amount; break;
			//case 4: offset.Z = 0; break;
			case 5: offset.Z = amount; break;
		}
		voxel_origin += offset;

		GD.Print("new origin: " + voxel_origin + " new dimensions: " + voxel_map[0][0].Count + "," + voxel_map[0].Count + "," + voxel_map.Count);
	}

	// valid FaceStates:
	// NONE - no tris
	// SOLID - two tris covering the whole area
	// STAIR - side view of 4 steps ascending towards the top right
	// ARCH - curved overhang with the full corner in the top right
	// NARROW_ARCH - curved section at the top
	// SLOPE - sloped overhang with the full corner in the top right
	// SINGLE_STEP - quarter-block-high quad at the bottom of the area

	private enum FaceState
	{
		NONE		= 0b00000000,
		SOLID		= 0b00000010, // last bit reserved for direction (0 = facing along positive axis, 1 = along negative axis)
		STAIR		= 0b00001000, // two bits reserved for flipping on different axes, last for facing
		ARCH		= 0b00010000, // two bits reserved for flipping, last for facing
		NARROW_ARCH = 0b00100000, // one bit reserved for flipping, last for facing
		SLOPE		= 0b01000000, // two bits reserved for flipping, last for facing
		SINGLE_STEP = 0b10000000, // one bit reserved for flipping, last for facing
	}

    static FaceState[] faces =
    {
        FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE,						// EMPTY
		FaceState.SOLID, FaceState.SOLID, FaceState.SOLID, FaceState.SOLID, FaceState.SOLID, FaceState.SOLID,				// SOLID
		FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE, // invalid
		FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE, FaceState.NONE, // invalid
		FaceState.SOLID, FaceState.SOLID, FaceState.SOLID, FaceState.NONE, FaceState.NARROW_ARCH, FaceState.NARROW_ARCH,	// NARROW_ARCH
    };

    private FaceState GetFace(Voxel vox, int dir)
	{
		return faces[((int)vox * 6) + dir];
	}

	private FaceState GetFaceState(Voxel vox_before, Voxel vox_after, int dir)
	{
		// TODO: look up the face shape of that particular side of each voxel
		// TODO: look up the combination of voxels in the lookup table
		// TODO: account for the direcion (i.e. which side of the voxel we care about) being different
	}

	private void RealizeFace(FaceState state, int dir, Vector3I cell, ref List<Vector3> verts, ref List<Vector3> norms)
	{
		// TODO: convert a face state, direction, and cell center position into a handful of triangles
	}

	public void Rebuild()
	{
		// TODO:
		List<Vector3> verts = new List<Vector3>();
		List<Vector3> norms = new List<Vector3>();

		// map out the face states between cells
		// convert this to rudimentary geometry
		for (int i = 0; i < voxel_map.Count; i++)
		{
			for (int j = 0; j < voxel_map[i].Count; j++)
			{
				for (int k = 0; k < voxel_map[i][j].Count; k++)
				{
					Voxel v = voxel_map[i][j][k];
					Voxel v_before = Voxel.EMPTY;

					// look along x axis and evaluate the faces between us and the cell before
					if (k > 0)
                        v_before = voxel_map[i][j][k - 1];
					RealizeFace(GetFaceState(v_before, v, 0), 0, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);

					// look along y axis and do the same
					if (j > 0)
						v_before = voxel_map[i][j - 1][k];
					RealizeFace(GetFaceState(v_before, v, 2), 2, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);

                    // look along z axis and do the same
                    if (i > 0)
                        v_before = voxel_map[i - 1][j][k];
                    RealizeFace(GetFaceState(v_before, v, 4), 4, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);

                    // if we're at the far edge on each axis, do an extra one
                    if (k == voxel_map[i][j].Count - 1)
						RealizeFace(GetFaceState(v, Voxel.EMPTY, 0), 0, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);
                    if (j == voxel_map[i].Count - 1)
                        RealizeFace(GetFaceState(v, Voxel.EMPTY, 2), 2, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);
                    if (i == voxel_map.Count - 1)
                        RealizeFace(GetFaceState(v, Voxel.EMPTY, 4), 4, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);
                }
            }
		}
		// deduplicate vertices
		// convert to immediatemesh
	}
}
