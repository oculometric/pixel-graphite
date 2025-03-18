using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public struct Voxel
{
	public VoxelType type;			// index into voxel type array
	public byte orientation = 0;	// packed, 2 bits represent four rotations around vertical, 1 bit represents flipping upsde down

	public Voxel(VoxelType t, byte o)
	{
		type = t;
		orientation = o;
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

	[Export] public VoxelType[] voxel_types { get; private set; }
    [Export] public float voxel_size = 0.8f;
    [Export] uint initial_size = 7;
	[Export] CollisionShape3D collider;

	public override void _Ready()
	{
		Mesh = new ArrayMesh();
		collider.Shape = new ConcavePolygonShape3D();

		for (int i = 0; i < voxel_types.Length; i++)
		{
			if (voxel_types[i].geometry == null)
				GD.Print("voxel name: " + voxel_types[i].name);
			else
				GD.Print("voxel name: " + voxel_types[i].name + " geometry size: " + voxel_types[i].geometry.SurfaceGetArrays(0)[0].AsVector3Array().Length);
		}

		if (voxel_map != null)
			return;

        // init voxel map
        voxel_map = new List<List<List<Voxel>>>();
		for (int i = 0; i < initial_size; i++)
		{
			List<List<Voxel>> area = new List<List<Voxel>>();
			for (int j = 0; j < initial_size; j++)
			{
				List<Voxel> row = new List<Voxel>();
				for (int k = 0; k < initial_size; k++)
					row.Add(new Voxel(voxel_types[0], 0));
				area.Add(row);
			}
			voxel_map.Add(area);
		}

		int o = (((int)initial_size - 1) / 2);
		voxel_origin = new Vector3I(o, o, o);
		//GD.Print("origin: " + voxel_origin + " dimensions: " + voxel_map[0][0].Count + "," + voxel_map[0].Count + "," + voxel_map.Count);

		SetCellValue(new Vector3I(0, 0, 0), new Voxel(voxel_types[1], 0));
		Rebuild();
	}

	private void WriteInt32(Int32 i, ref byte[] arr, uint offset)
	{
        arr[offset] = (byte)(i & 0xFF);
        arr[offset + 1] = (byte)((i >> 8) & 0xFF);
        arr[offset + 2] = (byte)((i >> 16) & 0xFF);
        arr[offset + 3] = (byte)((i >> 24) & 0xFF);
    }

	public byte[] Serialise()
	{
		// header consists of:
		// 4 byte x size
		// 4 byte y size
		// 4 byte z size
		// 4 byte x origin
		// 4 byte y origin
		// 4 byte z origin
		// 4 byte offset of the start of the name index
		// 4 byte size of the name index
		// 4 byte offset of the start of the data
		// 4 byte size of the data
		
		// 4 byte padding

		// name index entry consists of:
		// 4 byte size of entry
		// string name

		// 4 byte padding

		// data consists of:
		// 1 byte orientation
		// 3 byte index into name index

		List<string> voxel_names = new List<string>();
		foreach (VoxelType type in voxel_types)
			voxel_names.Add(type.name);

		List<byte> index = new List<byte>();
		foreach (string name in voxel_names)
		{
			Int32 entry_size = name.Length;
			index.Add((byte)((entry_size >> 0) & 0xFF));
			index.Add((byte)((entry_size >> 8) & 0xFF));
			index.Add((byte)((entry_size >> 16) & 0xFF));
			index.Add((byte)((entry_size >> 24) & 0xFF));

			index.AddRange(name.ToAsciiBuffer());
        }
		Int32 index_size = index.Count;

		Int32 data_size = 4 * voxel_map.Count * voxel_map[0].Count * voxel_map[0][0].Count;
		byte[] data = new byte[data_size];
        uint offset = 0;
		for (int i = 0; i < voxel_map.Count; i++)
		{
			for (int j = 0; j < voxel_map[0].Count; j++)
			{
				for (int k = 0; k < voxel_map[0][0].Count; k++)
				{
					Int32 data_element = (voxel_map[i][j][k].orientation << 24) | (voxel_names.FindIndex(a => a == voxel_map[i][j][k].type.name) & 0x00FFFFFF);
					WriteInt32(data_element, ref data, offset);
                    offset += 4;
				}
			}
		}

		Int32 header_size = 4 * 10;
        byte[] header = new byte[header_size];
		WriteInt32(voxel_map[0][0].Count, ref header, 0);
		WriteInt32(voxel_map[0].Count, ref header, 4);
		WriteInt32(voxel_map.Count, ref header, 8);
        WriteInt32(voxel_origin.X, ref header, 12);
        WriteInt32(voxel_origin.Y, ref header, 16);
        WriteInt32(voxel_origin.Z, ref header, 20);
        Int32 index_offset = header_size + 4;
        WriteInt32(index_offset, ref header, 24);
		WriteInt32(index_size, ref header, 28);
		Int32 data_offset = index_offset + index_size + 4;
        WriteInt32(data_offset, ref header, 32);
		WriteInt32(data_size, ref header, 36);

		byte[] final_array = new byte[header_size + 4 + index_size + 4 + data_size + 4];
		header.CopyTo(final_array, 0);
		WriteInt32(0x4a4a4a4a, ref final_array, (uint)(index_offset - 4));
		index.CopyTo(final_array, index_offset);
		WriteInt32(0x4a4a4a4a, ref final_array, (uint)(data_offset - 4));
		data.CopyTo(final_array, data_offset);
		WriteInt32(0x4a4a4a4a, ref final_array, (uint)(data_offset + data_size));

		return final_array;
    }

    private Int32 ReadInt32(ref byte[] arr, uint offset)
    {
        return (arr[offset + 3] << 24) | (arr[offset + 2] << 16) | (arr[offset + 1] << 8) | arr[offset];
    }

    public void Deserialise(byte[] bytes)
	{
		voxel_map = new List<List<List<Voxel>>>();

		Int32 header_size = 4 * 10;
		if (bytes.Length < header_size)
			throw new Exception("invalid data length");
		Int32 size_x = ReadInt32(ref bytes, 0);
		Int32 size_y = ReadInt32(ref bytes, 4);
		Int32 size_z = ReadInt32(ref bytes, 8);
		Int32 origin_x = ReadInt32(ref bytes, 12);
		Int32 origin_y = ReadInt32(ref bytes, 16);
		Int32 origin_z = ReadInt32(ref bytes, 20);
		Int32 index_offset = ReadInt32(ref bytes, 24);
		Int32 index_size = ReadInt32(ref bytes, 28);
		Int32 data_offset = ReadInt32(ref bytes, 32);
		Int32 data_size = ReadInt32(ref bytes, 36);

		if (index_offset + index_size > bytes.Length)
			throw new Exception("invalid data length");
		if (data_offset + data_size > bytes.Length)
			throw new Exception("invalid data length");

		voxel_origin = new Vector3I(origin_x, origin_y, origin_z);

		List<string> type_names = new List<string>();
		List<VoxelType> types = new List<VoxelType>();
		for (uint b = (uint)index_offset; b < index_offset + index_size; b++)
		{
			Int32 entry_length = ReadInt32(ref bytes, b);
			string s = "";
			b += 4;
			for (int i = 0; i < entry_length; i++)
			{
				s += (char)bytes[b];
				b++;
			}
			type_names.Add(s);
			b--;
			VoxelType v = null;
			foreach (VoxelType vt in voxel_types)
			{
				if (vt.name == s)
				{
					v = vt;
					break;
				}
			}
			types.Add(v);
		}

        Int32 d_size = 4 * size_x * size_y * size_z;
		if (d_size != data_size)
			throw new Exception("data size does not match");

        uint offset = (uint)data_offset;
		for (int i = 0; i < size_z; i++)
		{
			voxel_map.Add(new List<List<Voxel>>());
			for (int j = 0; j < size_y; j++)
			{
				voxel_map[i].Add(new List<Voxel>());
				for (int k = 0; k < size_x; k++)
				{
					byte orientation = bytes[offset + 3];
					Int32 index = ReadInt32(ref bytes, offset) & 0x00FFFFFF;
					Voxel v = new Voxel(types[index], orientation);
					voxel_map[i][j].Add(v);
					offset += 4;
				}
			}
		}

		GD.Print("successfully loaded mesh");
		Rebuild();
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
		try
		{
			voxel_map[array_z_pos][array_y_pos][array_x_pos] = type;
		} catch (ArgumentOutOfRangeException)
		{
			PrintMap();
		}
	}

	private void PrintMap()
	{
		GD.Print("number of slices along Z: " + voxel_map.Count);
		for (int i = 0; i < voxel_map.Count; i++)
		{
			GD.Print("  number of rows in slice " + i + ": " + voxel_map[i].Count);
			for (int j = 0; j < voxel_map[i].Count; j++)
			{
				GD.Print("    number of tiles in row " + j + ": " + voxel_map[i][j].Count);
			}
		}

		GD.Print("done");
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
								voxel_map[i][j].Add(new Voxel(voxel_types[0], 0));
							else if (dir == Axis.NEG_X)
								voxel_map[i][j].Insert(0, new Voxel(voxel_types[0], 0));
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
						for (int j = 0; j < voxel_map[i][0].Count; j++)
							arr.Add(new Voxel(voxel_types[0], 0));
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
							arr2.Add(new Voxel(voxel_types[0], 0));
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

	private Vector3 Swizzle(Vector3 vec, byte orientation)
	{
		float angle = 0;
		switch (orientation & 0b11)
		{
			case 0b00: angle = 0; break;
			case 0b01: angle = Mathf.Pi * 0.5f; break;
			case 0b10: angle = Mathf.Pi; break;
			case 0b11: angle = Mathf.Pi * 1.5f; break;
		}

		vec = vec.Rotated(Vector3.Up, angle);
		vec.Y *= ((orientation & 0b100) > 0) ? -1 : 1;

		return vec;
	}

	public void Rebuild()
	{
		(Mesh as ArrayMesh).ClearSurfaces();
		Vector3[] box_collider =
		{
			new Vector3(-0.4f, 0.4f, 0.4f),
			new Vector3(0.4f, 0.4f, 0.4f),
			new Vector3(0.4f, -0.4f, 0.4f),

			new Vector3(0.4f, -0.4f, 0.4f),
			new Vector3(-0.4f, -0.4f, 0.4f),
			new Vector3(-0.4f, 0.4f, 0.4f),

			new Vector3(-0.4f, 0.4f, -0.4f),
			new Vector3(-0.4f, 0.4f, 0.4f),
			new Vector3(-0.4f, -0.4f, 0.4f),

            new Vector3(-0.4f, -0.4f, 0.4f),
			new Vector3(-0.4f, -0.4f, -0.4f),
			new Vector3(-0.4f, 0.4f, -0.4f),

			new Vector3(0.4f, 0.4f, -0.4f),
			new Vector3(-0.4f, 0.4f, -0.4f),
			new Vector3(-0.4f, -0.4f, -0.4f),

            new Vector3(-0.4f, -0.4f, -0.4f),
			new Vector3(0.4f, -0.4f, -0.4f),
            new Vector3(0.4f, 0.4f, -0.4f),

			new Vector3(0.4f, 0.4f, 0.4f),
			new Vector3(0.4f, 0.4f, -0.4f),
			new Vector3(0.4f, -0.4f, -0.4f),

            new Vector3(0.4f, -0.4f, -0.4f),
			new Vector3(0.4f, -0.4f, 0.4f),
            new Vector3(0.4f, 0.4f, 0.4f),

			new Vector3(0.4f, 0.4f, 0.4f),
			new Vector3(-0.4f, 0.4f, 0.4f),
			new Vector3(-0.4f, 0.4f, -0.4f),

			new Vector3(-0.4f, 0.4f, -0.4f),
			new Vector3(0.4f, 0.4f, -0.4f),
			new Vector3(0.4f, 0.4f, 0.4f),

			new Vector3(0.4f, -0.4f, -0.4f),
			new Vector3(-0.4f, -0.4f, -0.4f),
			new Vector3(-0.4f, -0.4f, 0.4f),

            new Vector3(-0.4f, -0.4f, 0.4f),
			new Vector3(0.4f, -0.4f, 0.4f),
            new Vector3(0.4f, -0.4f, -0.4f),
        };

        List<Vector3> verts = new List<Vector3>();

		for (int i = 0; i < voxel_map.Count; i++)
		{
			for (int j = 0; j < voxel_map[i].Count; j++)
			{
				for (int k = 0; k < voxel_map[i][j].Count; k++)
				{
					Voxel v = voxel_map[i][j][k];
					VoxelType type = v.type;
					Mesh geom = type.geometry;
					if (geom == null) continue;

					Godot.Collections.Array arrays = geom.SurfaceGetArrays(0);
					Vector3[] v_arr = arrays[(int)(ArrayMesh.ArrayType.Vertex)].AsVector3Array();
					Vector3[] n_arr = arrays[(int)(ArrayMesh.ArrayType.Normal)].AsVector3Array();
                    for (int t = 0; t < v_arr.Length; t++)
					{
						v_arr[t] = Swizzle(v_arr[t], v.orientation);
						n_arr[t] = Swizzle(n_arr[t], v.orientation);
						v_arr[t] += new Vector3(k - voxel_origin.X, j - voxel_origin.Y, i - voxel_origin.Z) * voxel_size;
					}
					arrays[(int)(ArrayMesh.ArrayType.Vertex)] = v_arr;
					arrays[(int)(ArrayMesh.ArrayType.Normal)] = n_arr;

					Vector3[] coll_arr = new Vector3[box_collider.Length];
					box_collider.CopyTo(coll_arr, 0);
                    for (int t = 0; t < coll_arr.Length; t++)
                        coll_arr[t] += new Vector3(k - voxel_origin.X, j - voxel_origin.Y, i - voxel_origin.Z) * voxel_size;
                    verts.AddRange(coll_arr);

                    (Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
				}
			}
		}

		(collider.Shape as ConcavePolygonShape3D).SetFaces(verts.ToArray());
		(Mesh as ArrayMesh).ShadowMesh = (Mesh.Duplicate() as ArrayMesh);
	}
}
