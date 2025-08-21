using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public interface Serialiseable
{
	public byte[] GetBytes();
	public void SetBytes(byte[] bytes);
}

public struct VoxelDefinition
{
	public byte solid_face_flags; // (LSB to MSB): +x, -x, +y, -y, +z, -z
	
	// TODO: geometry, symmetry?

	public VoxelDefinition(byte face_solidity)
	{
		solid_face_flags = face_solidity;
	}
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

	public void Export(string path)
	{
		byte[] data = MeshExporter.ExportObj((Mesh as ArrayMesh));
		Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
		file.StoreBuffer(data);
		file.Close();
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

	private static VoxelDefinition[] voxel_definitions =
	{
		new VoxelDefinition(0),
		new VoxelDefinition(0b00111111),
		new VoxelDefinition(0b00100100),
		// TODO: the rest of the voxel types
	};

	private byte GetFaceFilledFlags(Voxel2 vox)
	{
		// returned byte consists of backed bools of whether the 6 cardinal directions have filled faces
		if (vox.id >= voxel_definitions.Length)
			return 0;
		byte natural_flags = voxel_definitions[vox.id].solid_face_flags;
		bool px = (natural_flags & 0b1) > 0;
		bool nx = (natural_flags & 0b10) > 0;
		bool py = (natural_flags & 0b100) > 0;
		bool ny = (natural_flags & 0b1000) > 0;
		bool pz = (natural_flags & 0b10000) > 0;
		bool nz = (natural_flags & 0b100000) > 0;

		bool spx = px;
		bool snx = nx;
		bool spy = py;
		bool sny = ny;
		bool spz = pz;
		bool snz = nz;

		switch (vox.orientation & 0b11)
		{
			case 0b00: break;
			case 0b01: spx = pz; snx = nz; spz = nx; snz = px; break;
			case 0b10: spx = nx; snx = px; spz = nz; snz = pz; break;
			case 0b11: spx = nz; snx = pz; spz = px; snz = nx; break;
		}
		if ((vox.orientation & 0b100) > 0)
			{ spz = nz; snz = pz; }

		byte swizzled_flags = (byte)((spx ? 0b00000001 : 0)
								   | (snx ? 0b00000010 : 0)
								   | (spy ? 0b00000100 : 0)
								   | (sny ? 0b00001000 : 0)
								   | (spz ? 0b00010000 : 0)
								   | (snz ? 0b00100000 : 0));

		return swizzled_flags;
	}

	private void AddFaces(Voxel2 vox, byte directions, ref List<Vector3> verts)
	{
		// TODO: get the geometry for the given voxel, swizzle it, and selectively apply it to the vertex array depending on which edges have flags
		
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

		List<Vector3> collision_verts = new List<Vector3>();
        List<Vector3> verts = new List<Vector3>();

		Vector3I min = map.GetMin();
		Vector3I max = map.GetMax();

		for (int i = min.X; i <= max.X; i++)
		{
			for (int j = min.Y; j <= max.Y; j++)
			{
				for (int k = min.Z; k <= max.Z; k++)
				{
					Voxel2 v_current = map[i, j, k];
					Voxel2 v_next_x = (i != max.X) ? map[i + 1, j, k] : new Voxel2(0, 0);
					byte sff_next_x = GetFaceFilledFlags(v_next_x);
					Voxel2 v_next_y = (j != max.Y) ? map[i, j + 1, k] : new Voxel2(0, 0);
					byte sff_next_y = GetFaceFilledFlags(v_next_y);
					Voxel2 v_next_z = (k != max.Z) ? map[i, j, k + 1] : new Voxel2(0, 0);
					byte sff_next_z = GetFaceFilledFlags(v_next_z);
					Voxel2 v_last_x = (i != min.X) ? map[i - 1, j, k] : new Voxel2(0, 0);
					byte sff_last_x = GetFaceFilledFlags(v_last_x);
					Voxel2 v_last_y = (j != min.Y) ? map[i, j - 1, k] : new Voxel2(0, 0);
					byte sff_last_y = GetFaceFilledFlags(v_last_y);
					Voxel2 v_last_z = (k != min.Z) ? map[i, j, k - 1] : new Voxel2(0, 0);
					byte sff_last_z = GetFaceFilledFlags(v_last_z);

					byte faces_to_add = 0;

					// check face-filled-ness of previous blocks
					if ((sff_last_x & 0b00000001) == 0)
						faces_to_add |= 0b00000010;
					if ((sff_last_y & 0b00000100) == 0)
						faces_to_add |= 0b00001000;
					if ((sff_last_z & 0b00010000) == 0)
						faces_to_add |= 0b00100000;

					// check face-filled-ness of next blocks
					if ((sff_next_x & 0b00000010) == 0)
						faces_to_add |= 0b00000001;
					if ((sff_next_y & 0b00001000) == 0)
						faces_to_add |= 0b00000100;
					if ((sff_next_z & 0b00100000) == 0)
						faces_to_add |= 0b00010000;

					// check if all side faces are set to not be drawn
					if (faces_to_add != 0b00000000)
						faces_to_add |= 0b01111111;

					AddFaces(v_current, faces_to_add, ref verts);
					
					Vector3[] collider = new Vector3[box_collider.Length];
					box_collider.CopyTo(collider, 0);
					for (int t = 0; t < collider.Length; t++)
						collider[t] += new Vector3(i, j, k) * voxel_size;
					collision_verts.AddRange(collider);
				}
			}
		}

		// TODO: deduplicate vertices using voxeliser method
		// TODO: create mesh
		// TODO: normals!!!

		(collider.Shape as ConcavePolygonShape3D).SetFaces(collision_verts.ToArray());
		(Mesh as ArrayMesh).ShadowMesh = (Mesh.Duplicate() as ArrayMesh);
	}
}
