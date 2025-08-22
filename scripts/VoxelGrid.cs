using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public interface Serialiseable
{
	public byte[] GetBytes();
	public void SetBytes(byte[] bytes);
}

public class VoxelDescription
{
	public byte solid_face_flags; // (LSB to MSB): +x, -x, +y, -y, +z, -z
	public int total_tris;
	public Vector3[] face_px = [];
	public Vector3[] face_nx = [];
	public Vector3[] face_py = [];
	public Vector3[] face_ny = [];
	public Vector3[] face_pz = [];
	public Vector3[] face_nz = [];
	public Vector3[] interior = [];

	public VoxelDescription(byte face_solidity)
	{
		solid_face_flags = face_solidity;
	}
}

public struct Voxel : Serialiseable
{
	public byte id = 0;				// id of the voxel type
	public byte orientation = 0;	// packed, 2 bits represent four rotations around vertical, 1 bit represents flipping upsde down

	public Voxel(byte _id, byte _orient)
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
	private GridMap3D<Voxel> map;

	[Export] public VoxelType[] voxel_types { get; private set; }
    [Export] public float voxel_size = 0.8f;
    [Export] uint initial_size = 7;
	[Export] CollisionShape3D collider;

	private List<VoxelDescription> voxel_descriptions;

	public override void _Ready()
	{
		Mesh = new ArrayMesh();
		collider.Shape = new ConcavePolygonShape3D();

		InitialiseVoxelDescriptions();

		if (map != null)
			return;

        // init voxel map
		map = new GridMap3D<Voxel>(Vector3I.One * (int)initial_size, new Voxel(0, 0));
		SetCellValue(new Vector3I(0, 0, 0), new Voxel(1, 0));
		Rebuild();
	}

	public void InitialiseVoxelDescriptions()
	{
		voxel_descriptions = new List<VoxelDescription>();
		foreach (VoxelType type in voxel_types)
		{
			VoxelDescription vd = new VoxelDescription(type.solid_face_flags);
			if (type.geometry != null && type.geometry.GetSurfaceCount() > 0)
			{
				Godot.Collections.Array arrays = type.geometry.SurfaceGetArrays(0);
				int[] indices = arrays[(int)ArrayMesh.ArrayType.Index].AsInt32Array();
				Vector3[] vertices = arrays[(int)ArrayMesh.ArrayType.Vertex].AsVector3Array();
				Vector3[] normals = arrays[(int)ArrayMesh.ArrayType.Normal].AsVector3Array();
				Vector2[] uvs = arrays[(int)ArrayMesh.ArrayType.TexUV].AsVector2Array();

				vd.total_tris = indices.Length / 3;

				List<Vector3> face_px = new List<Vector3>();
				List<Vector3> face_nx = new List<Vector3>();
				List<Vector3> face_py = new List<Vector3>();
				List<Vector3> face_ny = new List<Vector3>();
				List<Vector3> face_pz = new List<Vector3>();
				List<Vector3> face_nz = new List<Vector3>();
				List<Vector3> face_int = new List<Vector3>();

				for (int o = 0; o < indices.Length - 2; o += 3)
				{
					int i1 = indices[o];
					int i2 = indices[o + 1];
					int i3 = indices[o + 2];

					int u1s = Mathf.FloorToInt(uvs[i1].X * 8);
					int u2s = Mathf.FloorToInt(uvs[i2].X * 8);
					int u3s = Mathf.FloorToInt(uvs[i3].X * 8);
					if (u1s != u2s || u1s != u3s)
						u1s = 6;

					ref List<Vector3> target_list = ref face_int;
					switch (u1s)
					{
						case 0: target_list = ref face_px; break;
						case 1: target_list = ref face_nx; break;
						case 2: target_list = ref face_py; break;
						case 3: target_list = ref face_ny; break;
						case 4: target_list = ref face_pz; break;
						case 5: target_list = ref face_nz; break;
					}

					target_list.AddRange([vertices[i1], vertices[i2], vertices[i3],
										normals[i1], normals[i2], normals[i3]]);
				}
				
				vd.face_px = face_px.ToArray();
				vd.face_nx = face_nx.ToArray();
				vd.face_py = face_py.ToArray();
				vd.face_ny = face_ny.ToArray();
				vd.face_pz = face_pz.ToArray();
				vd.face_nz = face_nz.ToArray();
				vd.interior = face_int.ToArray();
			}

			voxel_descriptions.Add(vd);

			GD.Print("generated " + type.name + " voxel with mask " + type.solid_face_flags + " and vert counts " + (vd.face_px.Length / 2) + ","
																												  + (vd.face_nx.Length / 2) + ","
																												  + (vd.face_py.Length / 2) + ","
																												  + (vd.face_ny.Length / 2) + ","
																												  + (vd.face_pz.Length / 2) + ","
																												  + (vd.face_nz.Length / 2) + ","
																												  + (vd.interior.Length / 2));
		}
	}

    public void SetCellValue(Vector3I position, Voxel value)
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
			map = new GridMap3D<Voxel>(save_data, new Voxel(0, 0));
		} catch (InvalidDataException)
        {
			// TODO: recreate old data format loader for backwards compat
			map = new GridMap3D<Voxel>(Vector3I.One * (int)initial_size, new Voxel(0, 0));
		} catch (Exception)
		{
			map = new GridMap3D<Voxel>(Vector3I.One * (int)initial_size, new Voxel(0, 0));
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

	private byte GetSwizzledFlags(byte flags, byte orientation)
	{
		byte natural_flags = flags;
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

		switch (orientation & 0b11)
		{
			case 0b00: break;
			case 0b01: spx = py; snx = ny; spy = nx; sny = px; break;
			case 0b10: spx = nx; snx = px; spy = ny; sny = py; break;
			case 0b11: spx = ny; snx = py; spy = px; sny = nx; break;
		}
		if ((orientation & 0b100) > 0)
			{ spz = nz; snz = pz; }

		byte swizzled_flags = (byte)((spx ? 0b00000001 : 0)
								   | (snx ? 0b00000010 : 0)
								   | (spy ? 0b00000100 : 0)
								   | (sny ? 0b00001000 : 0)
								   | (spz ? 0b00010000 : 0)
								   | (snz ? 0b00100000 : 0));

		return swizzled_flags;
	}

	private byte GetFaceFilledFlags(Voxel vox)
	{
		// returned byte consists of backed bools of whether the 6 cardinal directions have filled faces
		if (vox.id >= voxel_descriptions.Count)
			return 0;
		
		return GetSwizzledFlags(voxel_descriptions[vox.id].solid_face_flags, vox.orientation);
	}

	private void AddFaces(Voxel vox, Vector3 offset, byte directions, ref List<Vector3> verts, ref List<Vector3> norms)
	{
		if (vox.id >= voxel_descriptions.Count)
			return;
		
		VoxelDescription description = voxel_descriptions[vox.id];
		List<Vector3> all_data = new List<Vector3>();
		byte swizzled_directions = (byte)(GetSwizzledFlags(directions, vox.orientation) | (directions & 0b01000000));

		if ((swizzled_directions & 0b1) != 0) all_data.AddRange(description.face_px);
		if ((swizzled_directions & 0b10) != 0) all_data.AddRange(description.face_nx);
		if ((swizzled_directions & 0b100) != 0) all_data.AddRange(description.face_py);
		if ((swizzled_directions & 0b1000) != 0) all_data.AddRange(description.face_ny);
		if ((swizzled_directions & 0b10000) != 0) all_data.AddRange(description.face_pz);
		if ((swizzled_directions & 0b100000) != 0) all_data.AddRange(description.face_nz);
		if ((swizzled_directions & 0b1000000) != 0) all_data.AddRange(description.interior);

		if ((vox.orientation & 0b100) > 0)
		{
			for (int i = 0; i < all_data.Count - 2; i += 3)
			{
				Vector3 tmp = all_data[i];
				all_data[i] = all_data[i + 2];
				all_data[i + 2] = tmp;
			}
		}

		for (int i = 0; i < all_data.Count; i++)
		{
			Vector3 swizzled = Swizzle(all_data[i], vox.orientation);
			if (i % 6 < 3)
				verts.Add(swizzled + offset);
			else
				norms.Add(swizzled);
		}
	}

	private struct Vertex
	{
		public Vector3 position;
		public Vector3 normal;

        public Vertex(Vector3 pos, Vector3 norm)
        {
			position = pos;
			normal = norm;
        }
    }

	private void GenerateIndexedArrays(in List<Vector3> verts, in List<Vector3> norms,
									   out List<Vector3> verts_out, out List<Vector3> norms_out, out List<int> indices)
	{
		List<Vertex> result_vertices = new List<Vertex>();
        result_vertices.EnsureCapacity(verts.Count / 6);
        indices = new List<int>();
        indices.EnsureCapacity(verts.Count);
		float distance = 0.05f;
        float d2 = distance * distance;

        // convert a raw vertex array to a pair of (deduplicated) vertex and index arrays
        Dictionary<Vertex, int> vertex_refs = new Dictionary<Vertex, int>(/*new VectorComparer()*/);
        vertex_refs.EnsureCapacity(verts.Count / 3);
        LinkedList<Vertex> unmarked_verts = new LinkedList<Vertex>();

        // add all used vertices to vertex refs dictionary.
        for (int v = 0; v < verts.Count; v++)
		{
			Vertex vv = new Vertex(verts[v], norms[v]);
            vertex_refs[vv] = -1;
		}
        foreach (KeyValuePair<Vertex, int> vertex in vertex_refs)
            unmarked_verts.AddLast(vertex.Key);

        // perform proximity clustering
        while (unmarked_verts.Count > 0)
        {
            // select unmarked vertex, pointing its index to where the new vertex will be
            Vertex start_vert = unmarked_verts.First.Value;
            int index = result_vertices.Count;
            vertex_refs[start_vert] = index;
            unmarked_verts.RemoveFirst();

            // collect a cluster of nearby verts, marking each as used
            LinkedListNode<Vertex> lln = unmarked_verts.First;
            while (lln != null)
            {
                // calculate individual differences to perform early rejection
                float dx = lln.Value.position.X - start_vert.position.X;
                if (dx > distance) { lln = lln.Next; continue; }
                if (dx < -distance) { lln = lln.Next; continue; }
                float dy = lln.Value.position.Y - start_vert.position.Y;
                if (dy > distance) { lln = lln.Next; continue; }
                if (dy < -distance) { lln = lln.Next; continue; }
                float dz = lln.Value.position.Z - start_vert.position.Z;
                if (dz > distance) { lln = lln.Next; continue; }
                if (dz < -distance) { lln = lln.Next; continue; }
				if (lln.Value.normal.X != start_vert.normal.X
				 || lln.Value.normal.Y != start_vert.normal.Y
				 || lln.Value.normal.Z != start_vert.normal.Z)
					{ lln = lln.Next; continue; }
                if (((dx * dx) + (dy * dy) + (dz * dz)) < d2)
                {
                    // set the vertex ref for this vertex position to point to
                    // the last vertex in the result vertex array
                    vertex_refs[lln.Value] = index;
                    // remove this node from the linked list
                    LinkedListNode<Vertex> old = lln;
                    lln = lln.Next;
                    unmarked_verts.Remove(old);
                }
                else
                    lln = lln.Next;
            }

            // create the resulting vertex
            result_vertices.Add(start_vert);
        }

        // build the index array
        int r = 0;
        int i = 0;
        for (int v = 0; v < verts.Count; v++)
		{
			Vertex vertex = new Vertex(verts[v], norms[v]);
            int index = vertex_refs[vertex];
            indices.Add(index);
            i++;

            //if (index < 0 || index >= result_vertices.Count)
            //    GD.Print("oops");

            if (i == 3)
            {
                i = 0;
                int i0 = indices[indices.Count - 3];
                int i1 = indices[indices.Count - 2];
                int i2 = indices[indices.Count - 1];

                if (i0 == i1 || i0 == i2 || i1 == i2)
                {
                    indices.RemoveRange(indices.Count - 3, 3);
                    r++;
                }
            }
        }

		verts_out = new List<Vector3>(result_vertices.Count);
		norms_out = new List<Vector3>(result_vertices.Count);
		foreach (Vertex v in result_vertices)
		{
			verts_out.Add(v.position);
			norms_out.Add(v.normal);
		}
	}

	// TODO: improve indexing

	public void Rebuild()
	{
		GD.Print("rebuilding mesh based on " + map.GetSize().X + "x" + map.GetSize().Y + "x" + map.GetSize().Z + " voxel map...");
		var sw_total = Stopwatch.StartNew();

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
		List<Vector3> norms = new List<Vector3>();

		Vector3I min = map.GetMin();
		Vector3I max = map.GetMax();

		var sw_geom = Stopwatch.StartNew();
		int theoretical_total_tris = 0;
		for (int i = min.X; i <= max.X; i++)
		{
			for (int j = min.Y; j <= max.Y; j++)
			{
				for (int k = min.Z; k <= max.Z; k++)
				{
					Voxel v_current = map[i, j, k];
					if (v_current.id == 0 || v_current.id >= voxel_descriptions.Count)
						continue;
					Voxel v_next_x = (i != max.X) ? map[i + 1, j, k] : new Voxel(0, 0);
					byte sff_next_x = GetFaceFilledFlags(v_next_x);
					Voxel v_next_y = (j != max.Y) ? map[i, j + 1, k] : new Voxel(0, 0);
					byte sff_next_y = GetFaceFilledFlags(v_next_y);
					Voxel v_next_z = (k != max.Z) ? map[i, j, k + 1] : new Voxel(0, 0);
					byte sff_next_z = GetFaceFilledFlags(v_next_z);
					Voxel v_last_x = (i != min.X) ? map[i - 1, j, k] : new Voxel(0, 0);
					byte sff_last_x = GetFaceFilledFlags(v_last_x);
					Voxel v_last_y = (j != min.Y) ? map[i, j - 1, k] : new Voxel(0, 0);
					byte sff_last_y = GetFaceFilledFlags(v_last_y);
					Voxel v_last_z = (k != min.Z) ? map[i, j, k - 1] : new Voxel(0, 0);
					byte sff_last_z = GetFaceFilledFlags(v_last_z);

					byte faces_to_add = 0;

					// check face-filled-ness of previous blocks
					if ((sff_last_x & 0b00000001) == 0)
						faces_to_add |= 0b00000010;
					if ((sff_last_z & 0b00000100) == 0)
						faces_to_add |= 0b00000100;
					if ((sff_last_y & 0b00010000) == 0)
						faces_to_add |= 0b00100000;

					// check face-filled-ness of next blocks
					if ((sff_next_x & 0b00000010) == 0)
						faces_to_add |= 0b00000001;
					if ((sff_next_z & 0b00001000) == 0)
						faces_to_add |= 0b00001000;
					if ((sff_next_y & 0b00100000) == 0)
						faces_to_add |= 0b00010000;

					// check if all side faces are set to not be drawn
					if (faces_to_add != 0b00000000)
						faces_to_add |= 0b01000000;

					Vector3 voxel_offset = new Vector3(i, j, k) * voxel_size;
					AddFaces(v_current, voxel_offset, faces_to_add, ref verts, ref norms);
					
					theoretical_total_tris += voxel_descriptions[v_current.id].total_tris;

					Vector3[] collider = new Vector3[box_collider.Length];
					box_collider.CopyTo(collider, 0);
					for (int t = 0; t < collider.Length; t++)
						collider[t] += voxel_offset;
					collision_verts.AddRange(collider);
				}
			}
		}
		sw_geom.Stop();
		float geom_ms = (float)sw_geom.Elapsed.TotalMilliseconds;

		if (verts.Count > 0)
		{
			List<Vector3> final_verts;
			List<Vector3> final_norms;
			List<int> final_indices;

			var sw_index = Stopwatch.StartNew();
			GenerateIndexedArrays(in verts, in norms, out final_verts, out final_norms, out final_indices);
			sw_index.Stop();
			float index_ms = (float)sw_index.Elapsed.TotalMilliseconds;

			ArrayMesh am = Mesh as ArrayMesh;
			Godot.Collections.Array arrays = new Godot.Collections.Array();
			arrays.Resize((int)ArrayMesh.ArrayType.Max);
			arrays[(int)ArrayMesh.ArrayType.Vertex] = final_verts.ToArray();
			arrays[(int)ArrayMesh.ArrayType.Normal] = final_norms.ToArray();
			arrays[(int)ArrayMesh.ArrayType.Index] = final_indices.ToArray();
			am.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

			sw_total.Stop();
			float total_ms = (float)sw_total.Elapsed.TotalMilliseconds;

			GD.Print("verts without indexing: " + verts.Count + "; with indexing: " + final_verts.Count);
			GD.Print("tris without culling: " + theoretical_total_tris + "; with culling: " + (final_indices.Count / 3));
			GD.Print("took " + total_ms + "ms (" + geom_ms + "ms geometry, " + index_ms + "ms indexing)");
		}

		(collider.Shape as ConcavePolygonShape3D).SetFaces(collision_verts.ToArray());
		(Mesh as ArrayMesh).ShadowMesh = (Mesh.Duplicate() as ArrayMesh);
		GD.Print("done");
	}
}
