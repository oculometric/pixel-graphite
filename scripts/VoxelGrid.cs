using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public interface Serialiseable
{
	public byte[] GetBytes();
	public void SetBytes(byte[] bytes);
}

public struct Voxel2 : Serialiseable
{
	public byte id = 0;				// id of the voxel type
	public byte orientation = 0;	// packed, 2 bits represent four rotations around vertical, 1 bit represents flipping upsde down

	public Voxel2(byte _id, byte _orient)
	{
		id = _id;
		orientation = _orient;
	}

	public byte[] GetBytes()
	{
		return [id, orientation];
	}

	public void SetBytes(byte[] bytes)
	{
		id = bytes[0];
		orientation = bytes[1];
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

public class GridMap3D<T> where T : Serialiseable
{
	private Vector3I map_size;
	private Vector3I map_origin;
	private Vector3I map_min;
	private Vector3I map_max;
	private List<List<List<T>>> map;
	private T default_el;

	public GridMap3D(Vector3I initial_size, T default_element)
	{
		map_size = initial_size;
		map_origin = new Vector3I(0, 0, 0);
		map_min = map_origin;
		map_max = map_size - Vector3I.One;

		default_el = default_element;

		map = new List<List<List<T>>>(initial_size.Z);
		for (int k = 0; k < initial_size.Z; k++)
		{
			map.Add(new List<List<T>>(initial_size.Y));
			for (int j = 0; j < initial_size.Y; j++)
			{
				map[k].Add(new List<T>(initial_size.X));
				for (int i = 0; i < initial_size.X; i++)
				{
					map[k][j].Add(default_element);
				}
			}
		}
	}

	private bool IsInside(int x, int y, int z)
	{
		if (x < map_min.X || y < map_min.Y || z < map_min.Z)
			return false;
		if (x > map_max.X || y > map_max.Y || z > map_max.Z)
			return false;
		return true;
	}

	public T this[int x, int y, int z]
	{
		get 
		{ 
			if (!IsInside(x, y, z)) return default_el;
			return map[z + map_origin.Z][y + map_origin.Y][x + map_origin.X];
		}
		set
		{
			if (!IsInside(x, y, z)) ExtendGrid(x, y, z);
			map[z + map_origin.Z][y + map_origin.Y][x + map_origin.X] = value;
		}
	}

	public Vector3I GetSize() { return map_size; }

	public Vector3I GetMin() { return map_min; }

	public Vector3I GetMax() { return map_max; }

	private void ExtendGrid(int rx, int ry, int rz)
	{
		// extend all X arrays (innermost arrays) by the necessary amount
		if (rx > map_max.X)
		{
			int diff = rx - map_max.X;
			for (int k = 0; k < map_size.Z; k++)
			{
				for (int j = 0; j < map_size.Y; j++)
				{
					for (int i = 0; i < diff; i++) map[k][j].Add(default_el);
				}
			}
			map_max.X = rx;
			map_size.X += diff;
		}
		else if (rx < map_min.X)
		{
			int diff = map_min.X - rx;
			for (int k = 0; k < map_size.Z; k++)
			{
				for (int j = 0; j < map_size.Y; j++)
				{
					for (int i = 0; i < diff; i++) map[k][j].Insert(0, default_el);
				}
			}
			map_min.X = rx;
			map_origin.X += diff;
			map_size.X += diff;
		}

		// extend all Y arrays (middle arrays) by the necessary amount
		if (ry > map_max.Y)
		{
			int diff = ry - map_max.Y;
			for (int k = 0; k < map_size.Z; k++)
			{
				for (int j = 0; j < diff; j++)
				{
					List<T> l = new List<T>(map_size.X);
					for (int i = 0; i < map_size.X; i++) l.Add(default_el);
					map[k].Add(l);
				}
			}
			map_max.Y = ry;
			map_size.Y += diff;
		}
		else if (ry < map_min.Y)
		{
			int diff = map_min.Y - ry;
			for (int k = 0; k < map_size.Z; k++)
			{
				for (int j = 0; j < diff; j++)
				{
					List<T> l = new List<T>(map_size.X);
					for (int i = 0; i < map_size.X; i++) l.Add(default_el);
					map[k].Insert(0, l);
				}
			}
			map_min.Y = ry;
			map_origin.Y += diff;
			map_size.Y += diff;
		}

		// extend Z array (outer array) by the necessary amount
		if (rz > map_max.Z)
		{
			int diff = rz - map_max.Z;
			for (int k = 0; k < diff; k++)
			{
				List<List<T>> l = new List<List<T>>(map_size.Y);
				for (int j = 0; j < map_size.Y; j++)
				{
					List<T> l2 = new List<T>(map_size.X);
					for (int i = 0; i < map_size.X; i++) l2.Add(default_el);
					l.Add(l2);
				}
				map.Add(l);
			}
			map_max.Z = rz;
			map_size.Z += diff;
		}
		else if (rz < map_min.Z)
		{
			int diff = map_min.Z - rz;
			for (int k = 0; k < diff; k++)
			{
				List<List<T>> l = new List<List<T>>(map_size.Y);
				for (int j = 0; j < map_size.Y; j++)
				{
					List<T> l2 = new List<T>(map_size.X);
					for (int i = 0; i < map_size.X; i++) l2.Add(default_el);
					l.Add(l2);
				}
				map.Insert(0, l);
			}
			map_min.Z = rz;
			map_origin.Z += diff;
			map_size.Z += diff;
		}
	}

	private void WriteInt32(int i, ref List<byte> arr)
	{
        arr.Add((byte)(i & 0xFF));
        arr.Add((byte)((i >> 8) & 0xFF));
        arr.Add((byte)((i >> 16) & 0xFF));
        arr.Add((byte)((i >> 24) & 0xFF));
    }

	private int ReadInt32(in byte[] arr, uint offset)
    {
        return (arr[offset + 3] << 24) | (arr[offset + 2] << 16) | (arr[offset + 1] << 8) | arr[offset];
    }

	public byte[] Serialise()
	{
		// file structure
		// 4 byte signature

		// 4 byte x size
		// 4 byte y size
		// 4 byte z size

		// 4 byte x origin
		// 4 byte y origin
		// 4 byte z origin
		
		// 4 byte padding
		
		// 1 byte voxel type id
		// 1 byte orientation
		// ...

		List<byte> data = new List<byte>((4 * 8) + (2 * map_size.X * map_size.Y * map_size.Z));
		WriteInt32(0x4a6b7900, ref data);

		WriteInt32(map_size.X, ref data);
		WriteInt32(map_size.Y, ref data);
		WriteInt32(map_size.Z, ref data);

		WriteInt32(map_origin.X, ref data);
		WriteInt32(map_origin.Y, ref data);
		WriteInt32(map_origin.Z, ref data);

		WriteInt32(0, ref data);

		for (int k = 0; k < map_size.Z; k++)
		{
			for (int j = 0; j < map_size.Y; j++)
			{
				for (int i = 0; i < map_size.X; i++)
				{
					data.AddRange(map[k][j][i].GetBytes());
				}
			}
		}

		return data.ToArray();
	}

	public GridMap3D(byte[] serialised_data, T default_element)
	{
		if (serialised_data.Length < (4 * 8))
			throw new Exception("invalid voxel grid data");
		if (ReadInt32(in serialised_data, 0) != 0x4a6b7900)
			throw new InvalidDataException("invalid voxel data signature");

		map_size = new Vector3I(ReadInt32(in serialised_data, 4), ReadInt32(in serialised_data, 8), ReadInt32(in serialised_data, 12));
		map_origin = new Vector3I(ReadInt32(in serialised_data, 16), ReadInt32(in serialised_data, 20), ReadInt32(in serialised_data, 24));
		map_min = -map_origin;
		map_max = map_size - (Vector3I.One + map_origin);

		default_el = default_element;

		uint byte_offset = 32;
		map = new List<List<List<T>>>(map_size.Z);
		for (int k = 0; k < map_size.Z; k++)
		{
			map.Add(new List<List<T>>(map_size.Y));
			for (int j = 0; j < map_size.Y; j++)
			{
				map[k].Add(new List<T>(map_size.X));
				for (int i = 0; i < map_size.X; i++)
				{
					T value = default_el;
					value.SetBytes([serialised_data[byte_offset], serialised_data[byte_offset + 1]]);
					map[k][j].Add(value);
					byte_offset += 2;
				}
			}
		}
	}
}

public partial class VoxelGrid : MeshInstance3D
{
	private GridMap3D<Voxel2> map;

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

		if (map != null)
			return;

        // init voxel map
		map = new GridMap3D<Voxel2>(Vector3I.One * (int)initial_size, new Voxel2(0, 0));
		SetCellValue(new Vector3I(0, 0, 0), new Voxel2(1, 0));
		Rebuild();
	}

    public void SetCellValue(Vector3I position, Voxel2 value)
	{
		map[position.X, position.Y, position.Z] = value;
	}

	public void Save(string path)
	{
		byte[] save_data = map.Serialise();
		Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
		file.StoreBuffer(save_data);
		file.Close();
        GD.Print("successfully saved voxel data");
    }

    public void Load(string path)
	{
        Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
		if (file == null)
			return;
		byte[] save_data = file.GetBuffer((long)file.GetLength());
		file.Close();
		try
		{
			map = new GridMap3D<Voxel2>(save_data, new Voxel2(0, 0));
		} catch (InvalidDataException)
        {
			// TODO: recreate old data format loader for backwards compat
			map = new GridMap3D<Voxel2>(Vector3I.One * (int)initial_size, new Voxel2(0, 0));
		} catch (Exception)
		{
			map = new GridMap3D<Voxel2>(Vector3I.One * (int)initial_size, new Voxel2(0, 0));
		}
		Rebuild();
    }

	// private void PrintMap()
	// {
	// 	GD.Print("number of slices along Z: " + voxel_map.Count);
	// 	for (int i = 0; i < voxel_map.Count; i++)
	// 	{
	// 		GD.Print("  number of rows in slice " + i + ": " + voxel_map[i].Count);
	// 		for (int j = 0; j < voxel_map[i].Count; j++)
	// 		{
	// 			GD.Print("    number of tiles in row " + j + ": " + voxel_map[i][j].Count);
	// 		}
	// 	}

	// 	GD.Print("done");
	// }
	
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

		Vector3I min = map.GetMin();
		Vector3I max = map.GetMax();

		for (int i = min.X; i <= max.X; i++)
		{
			for (int j = min.Y; j <= max.Y; j++)
			{
				for (int k = min.Z; k <= max.Z; k++)
				{
					Voxel2 v = map[i, j, k];
					Mesh geom = voxel_types[v.id].geometry;
					if (geom == null) continue;

					Godot.Collections.Array arrays = geom.SurfaceGetArrays(0);
					Vector3[] v_arr = arrays[(int)(ArrayMesh.ArrayType.Vertex)].AsVector3Array();
					Vector3[] n_arr = arrays[(int)(ArrayMesh.ArrayType.Normal)].AsVector3Array();
                    for (int t = 0; t < v_arr.Length; t++)
					{
						v_arr[t] = Swizzle(v_arr[t], v.orientation);
						n_arr[t] = Swizzle(n_arr[t], v.orientation);
						v_arr[t] += new Vector3(i, j, k) * voxel_size;
					}
					if ((v.orientation & 0b100) > 0)
					{
						int[] i_arr = arrays[(int)(ArrayMesh.ArrayType.Index)].AsInt32Array();
						for (int t = 0; t < i_arr.Length - 2; t += 3)
						{
							int tmp = i_arr[t];
							i_arr[t] = i_arr[t + 2];
							i_arr[t + 2] = tmp;
						}
						arrays[(int)(ArrayMesh.ArrayType.Index)] = i_arr;
                    }
                    arrays[(int)(ArrayMesh.ArrayType.Vertex)] = v_arr;
					arrays[(int)(ArrayMesh.ArrayType.Normal)] = n_arr;

					Vector3[] coll_arr = new Vector3[box_collider.Length];
					box_collider.CopyTo(coll_arr, 0);
                    for (int t = 0; t < coll_arr.Length; t++)
                        coll_arr[t] += new Vector3(i, j, k) * voxel_size;
                    verts.AddRange(coll_arr);

                    (Mesh as ArrayMesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
				}
			}
		}

		(collider.Shape as ConcavePolygonShape3D).SetFaces(verts.ToArray());
		(Mesh as ArrayMesh).ShadowMesh = (Mesh.Duplicate() as ArrayMesh);
	}
}
