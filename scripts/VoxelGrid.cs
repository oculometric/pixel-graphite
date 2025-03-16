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

	public void SetCellValue(Vector3I position, Voxel type)
	{
		// calculate position in the array based on position and voxel_origin
		int array_x_pos = position.X + voxel_origin.X;
		int array_y_pos = position.Y + voxel_origin.Y;
		int array_z_pos = position.Z + voxel_origin.Z;

		//GD.Print("set voxel: " + position + " in arrays: " + array_x_pos + "," + array_y_pos + "," + array_z_pos);

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
						for (int j = 0; j < voxel_map[i].Count; j++)
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

		//GD.Print("new origin: " + voxel_origin + " new dimensions: " + voxel_map[0][0].Count + "," + voxel_map[0].Count + "," + voxel_map.Count);
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
