using Godot;
using System;
using System.Collections.Generic;

public struct Voxel
{
	public short type = 0;			// index into voxel type array
	public byte orientation = 0;	// packed, 2 bits represent four rotations around vertical, 1 bit represents flipping upsde down

	public Voxel(short t, byte o)
	{
		type = t;
		orientation = o;
	}
}

// NONE - no tris
// SOLID - two tris covering the whole area
// STAIR - side view of 4 steps ascending towards the top right
// ARCH - curved overhang with the full corner in the top right
// NARROW_ARCH - curved section at the top
// SLOPE - sloped overhang with the full corner in the top right
// SINGLE_STEP - quarter-block-high quad at the bottom of the area

public struct FaceType
{
	// TODO: geometry
	public byte symmetries = 0; // symmetries across the X,Y axes

	public FaceType(byte s)
	{
		symmetries = s;
	}
}

public partial class VoxelType : Resource
{
	public string name = "Empty";			// human readable name
	public short[] faces = new short[6];    // indices into face type array, +x,-x,+y,-y,+z,-z
	public byte[] faces_flipped = new byte[6]; // flip instructions across the X,Y axes for each face pattern
	// TODO: extra geometry

	public VoxelType(string n, short[] f, byte[] ff)
	{
		name = n;
		faces = f;
		faces_flipped = ff;
	}

	/**
	 * orientation 0b00:
	 *    -z
	 * -x    +x     01 23 45
	 *    +z
	 * 
	 * orientation 0b01:
	 *    -x
	 * +z    -z     54 23 01
	 *    +x
	 * 
	 * orientation 0b10:
	 *    +z
	 * +x    -x     10 23 54
	 *    -z
	 * 
	 * orientation 0b11:
	 *    +x
	 * -z    +z     45 23 10
	 *    -x
	 */


	public KeyValuePair<short, byte> GetFaceType(byte orientation, Axis dir)
	{
		int[] or01 = { 5, 4, 2, 3, 0, 1 };
		int[] or10 = { 1, 0, 2, 3, 5, 4 };
		int[] or11 = { 4, 5, 2, 3, 1, 0 };

		int actual_index = (int)dir;
		switch (orientation & 0b11)
		{
			case 0b00: actual_index = (int)dir; break;
			case 0b01: actual_index = or01[(int)dir]; break;
            case 0b10: actual_index = or10[(int)dir]; break;
            case 0b11: actual_index = or11[(int)dir]; break;
        }

		byte flipped = faces_flipped[actual_index];
		flipped = (byte)(flipped ^ (orientation & 0b100));
        return new KeyValuePair<short, byte>(faces[actual_index], flipped);
    }
}

public enum Axis
{
	POS_X = 0,
	NEG_X = 1,
	POS_Y = 2,
	NEG_Y = 3,
	POS_Z = 4,
	NEG_Z = 5
}

public partial class VoxelGrid : MeshInstance3D
{
	private List<List<List<Voxel>>> voxel_map;
	private Vector3I voxel_origin = new Vector3I(0, 0, 0);

	private List<VoxelType> voxel_types;
	private List<FaceType> face_types;

	[Export] uint initial_size = 7;

	public override void _Ready()
	{
		// create face types
		face_types = new List<FaceType>();
		face_types.Add(new FaceType(0b11));		// empty face type			0
		face_types.Add(new FaceType(0b11));     // solid face type			1
		face_types.Add(new FaceType(0b00));     // stair face type			2
		face_types.Add(new FaceType(0b00));     // arch face type			3
		face_types.Add(new FaceType(0b10));     // narrow arch face type	4
		face_types.Add(new FaceType(0b00));     // slope face type			5
		face_types.Add(new FaceType(0b10));		// single stair face type	6

		// create voxel types
		voxel_types = new List<VoxelType>();
        // empty voxel type
        voxel_types.Add(new VoxelType("empty", new short[] { 0, 0, 0, 0, 0, 0 }, new byte[] { 0, 0, 0, 0, 0, 0 }));
        // solid voxel type
		voxel_types.Add(new VoxelType("solid", new short[] { 1, 1, 1, 1, 1, 1 }, new byte[] { 0, 0, 0, 0, 0, 0 }));
		// stair voxel type
		voxel_types.Add(new VoxelType("stair", new short[] { 1, 6, 6, 1, 2, 2 }, new byte[] { 0, 0, 0, 0, 0, 2 }));
		// arch voxel type
		voxel_types.Add(new VoxelType("arch", new short[] { 1, 0, 1, 0, 3, 3 }, new byte[] { 0, 0, 0, 0, 0, 2 }));
		// narrow arch voxel type
		voxel_types.Add(new VoxelType("narrow_arch", new short[] { 1, 1, 1, 0, 4, 4 }, new byte[] { 0, 0, 0, 0, 0, 0 }));
		// slope voxel type
		voxel_types.Add(new VoxelType("slope", new short[] { 1, 0, 1, 0, 5, 5 }, new byte[] { 0, 0, 0, 0, 0, 2 }));

		// init voxel map
		voxel_map = new List<List<List<Voxel>>>();
		for (int i = 0; i < initial_size; i++)
		{
			List<List<Voxel>> area = new List<List<Voxel>>();

			for (int j = 0; j < initial_size; j++)
			{
				List<Voxel> row = new List<Voxel>();

				for (int k = 0; k < initial_size; k++)
				{
					row.Add(new Voxel());
				}

				area.Add(row);
			}

			voxel_map.Add(area);
		}

		int o = (((int)initial_size - 1) / 2);
		voxel_origin = new Vector3I(o, o, o);
		GD.Print("origin: " + voxel_origin + " dimensions: " + voxel_map[0][0].Count + "," + voxel_map[0].Count + "," + voxel_map.Count);

		SetCellValue(voxel_origin, new Voxel(1,0));
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
			ExtendInDirection(Axis.NEG_X, -array_x_pos);
			array_x_pos = position.X + voxel_origin.X;
		}
		if (array_x_pos >= voxel_map[0][0].Count)
			ExtendInDirection(Axis.POS_X, (array_x_pos - voxel_map[0][0].Count) + 1);
		if (array_y_pos < 0)
		{
			ExtendInDirection(Axis.NEG_Y, -array_y_pos);
			array_y_pos = position.Y + voxel_origin.Y;
		}
		if (array_y_pos >= voxel_map[0].Count)
			ExtendInDirection(Axis.POS_Y, (array_y_pos - voxel_map[0].Count) + 1);
		if (array_z_pos < 0)
		{
			ExtendInDirection(Axis.NEG_Z, -array_z_pos);
			array_z_pos = position.Z + voxel_origin.Z;
		}
		if (array_z_pos >= voxel_map.Count)
			ExtendInDirection(Axis.POS_Z, (array_z_pos - voxel_map.Count) + 1);

		// set value
		voxel_map[array_z_pos][array_y_pos][array_x_pos] = type;
	}
	
	public void ExtendInDirection(Axis dir, int amount)
	{
		// add new arrays/voxels at the beginning/end of existing arrays (depending on the direction)
		switch (dir)
		{
			case Axis.POS_X: // +x, innermost arrays
			case Axis.NEG_X: // -x
				for (int i = 0; i < voxel_map.Count; i++)
				{
					for (int j = 0; j < voxel_map[i].Count; j++)
					{
						for (int _ = 0; _ < amount; _++)
						{
							if (dir == Axis.POS_X)
								voxel_map[i][j].Add(new Voxel());
							else if (dir == Axis.NEG_X)
								voxel_map[i][j].Insert(0, new Voxel());
						}
					}
				}
				break;
			case Axis.POS_Y: // +y, middle arrays
			case Axis.NEG_Y: // -y
				for (int i = 0; i < voxel_map.Count; i++)
				{
					for (int _ = 0; _ < amount; _++)
					{
						List<Voxel> arr = new List<Voxel>();
						for (int j = 0; j < voxel_map[i].Count; j++)
							arr.Add(new Voxel());
						if (dir == Axis.POS_Y)
							voxel_map[i].Add(arr);
						else if (dir == Axis.NEG_Y)
							voxel_map[i].Insert(0, arr);
					}
				}
				break;
			case Axis.POS_Z: // +z, outermost arrays
			case Axis.NEG_Z: // -z
				for (int _ = 0; _ < amount; _++)
				{
					List<List<Voxel>> arr = new List<List<Voxel>>();
					for (int j = 0; j < voxel_map[0].Count; j++)
					{
						List<Voxel> arr2 = new List<Voxel>();
						for (int k = 0; k < voxel_map[0][j].Count; k++)
							arr2.Add(new Voxel());
						arr.Add(arr2);
					}
					if (dir == Axis.POS_Z)
						voxel_map.Add(arr);
					else if (dir == Axis.NEG_Z)
						voxel_map.Insert(0, arr);
				}
				break;
		}

		// update voxel origin if adding at the beginning
		Vector3I offset = Vector3I.Zero;
		switch (dir)
		{
			//case 0: offset.X = 0; break;
			case Axis.NEG_X: offset.X = amount; break;
			//case 2: offset.Y = 0; break;
			case Axis.NEG_Y: offset.Y = amount; break;
			//case 4: offset.Z = 0; break;
			case Axis.NEG_Z: offset.Z = amount; break;
		}
		voxel_origin += offset;

		GD.Print("new origin: " + voxel_origin + " new dimensions: " + voxel_map[0][0].Count + "," + voxel_map[0].Count + "," + voxel_map.Count);
	}

	// TODO: face type preference table (for when faces to not match, decide which to keep)
	private void RealizeFace(Voxel vox_before, Voxel vox_after, Axis dir, Vector3I cell, ref List<Vector3> verts, ref List<Vector3> norms)
	{
		if ((int)dir % 2 == 1)
			return;

		// look up the face shape of that particular side of each voxel
		KeyValuePair<short, byte> f_before = voxel_types[vox_before.type].GetFaceType(vox_before.orientation, dir);
		KeyValuePair<short, byte> f_after = voxel_types[vox_after.type].GetFaceType(vox_after.orientation, dir + 1);
		FaceType ft_before = face_types[f_before.Key];
		byte ff_before = f_before.Value;
		FaceType ft_after = face_types[f_after.Key];
		// flip the second one horizontally agian
		byte ff_after = (byte)(f_before.Value ^ 0b100);

		// if the faces are the same type, and the same flipped-ness (or symmetrical) on each axis, then we don't add this face
		if (f_before.Key == f_after.Key
		 && (((ff_before & 0b01) == (ff_after & 0b01)) || ((ft_before.symmetries & 0b01) > 0))
		 && (((ff_before & 0b10) == (ff_after & 0b10)) || ((ft_before.symmetries & 0b10) > 0)))
			return;

		// TODO: look up the combination of face shapes in the lookup table if they don't match
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

					Voxel v_before = new Voxel();

					// look along x axis and evaluate the faces between us and the cell before
					if (k > 0)
						v_before = voxel_map[i][j][k - 1];
					RealizeFace(v_before, v, Axis.POS_X, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);

					// look along y axis and do the same
					if (j > 0)
						v_before = voxel_map[i][j - 1][k];
					RealizeFace(v_before, v, Axis.POS_Y, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);

					// look along z axis and do the same
					if (i > 0)
						v_before = voxel_map[i - 1][j][k];
					RealizeFace(v_before, v, Axis.POS_Z, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);

					// if we're at the far edge on each axis, do an extra one
					if (k == voxel_map[i][j].Count - 1)
						RealizeFace(v, new Voxel(), Axis.POS_X, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);
					if (j == voxel_map[i].Count - 1)
						RealizeFace(v, new Voxel(), Axis.POS_Y, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);
					if (i == voxel_map.Count - 1)
						RealizeFace(v, new Voxel(), Axis.POS_Z, new Vector3I(k, j, i) - voxel_origin, ref verts, ref norms);

					// TODO: additional geometry
				}
			}
		}
		// deduplicate vertices
		// convert to immediatemesh
	}
}
