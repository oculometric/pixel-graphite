using Godot;
using System;
using System.Collections.Generic;

public partial class SaveManager : Node3D
{
    [Export] public VoxelGrid voxel_grid { get; private set; }
    [Export] public EditingUIController ui_controller { get; private set; }
    [Export] public float autosave_time = 10.0f;

    private string current_file_name = "Untitled";
    private bool has_been_saved = false;
    public bool has_unsaved_changes { get; private set; } = true;
    private Timer autosave_timer = null;

    public void UpdateTitle()
    {
        GetWindow().Title = "pixel graphite - " + current_file_name + (has_unsaved_changes ? "*" : "") + " (" + Engine.GetFramesPerSecond().ToString("000.0") + " fps)";
    }

    public void SetUnsavedFlag() { has_unsaved_changes = true; UpdateTitle(); }

    public override void _Ready()
    {
        UpdateTitle();
        ui_controller.save_callback = SaveData;
        ui_controller.load_callback = LoadData;
        GetWindow().CloseRequested += () =>
        {
            if (has_unsaved_changes)
                ui_controller.ShowConfirmDialog("discard unsaved changes?", "you have made changes which are not saved. are you sure you want to close without saving?", "discard changes", "cancel", DiscardAndQuit);
            else
                GetTree().Quit();
        };
        autosave_timer = new Timer();
        AddChild(autosave_timer);
        autosave_timer.Timeout += () =>
        {
            if (!has_unsaved_changes)
            {
                autosave_timer.Start(autosave_time);
                return;
            }

            string file_name_base = current_file_name.GetFile().GetBaseName();
            string autosave_name = "user://autosave/" + file_name_base + "_autosave.dat";
            Godot.DirAccess.Open("user://").MakeDir("autosave");
            WriteSaveFile(autosave_name);
            autosave_timer.Start(autosave_time);
        };
        autosave_timer.Start(autosave_time);
    }

    public void CallSave(bool save_as)
    {
        if (has_been_saved && !save_as)
            SaveData(current_file_name);
        else
            ui_controller.ShowSaveDialog(current_file_name);
    }

    public void CallLoad()
    {
        if (has_unsaved_changes)
            ui_controller.ShowConfirmDialog("discard unsaved changes?", "you have made changes which are not saved. are you sure you want to discard them and open another file?", "discard changes", "cancel", ConfirmDiscard);
        else
            ui_controller.ShowLoadDialog(current_file_name);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            InputEventKey key = @event as InputEventKey;
            if (!key.CtrlPressed)
                return;
            if (key.IsPressed())
            {
                if (key.Keycode == Key.S)
                    CallSave(key.ShiftPressed);
                else if (key.Keycode == Key.O)
                    CallLoad();
            }
        }
    }

    public void DiscardAndQuit()
    {
        GetTree().Quit();
    }

    public void ConfirmDiscard()
    {
        ui_controller.ShowLoadDialog(current_file_name);
    }

    public void SaveData(string file)
    {
        WriteSaveFile(file);
        has_been_saved = true;
        current_file_name = file;
        has_unsaved_changes = false;
        autosave_timer.Start(autosave_time);
        UpdateTitle();
    }

    public void LoadData(string file)
    {
        ReadSaveFile(file);
        has_been_saved = true;
        current_file_name = file;
        has_unsaved_changes = false;
        UpdateTitle();
    }

    private void WriteInt32(int i, ref List<byte> arr)
    {
        arr.Add((byte)(i & 0xFF));
        arr.Add((byte)((i >> 8) & 0xFF));
        arr.Add((byte)((i >> 16) & 0xFF));
        arr.Add((byte)((i >> 24) & 0xFF));
    }

    private void WriteInt16(int i, ref List<byte> arr)
    {
        arr.Add((byte)(i & 0xFF));
        arr.Add((byte)((i >> 8) & 0xFF));
    }

    private void WriteUInt32(uint i, ref List<byte> arr)
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

    private uint ReadUInt32(in byte[] arr, uint offset)
    {
        return ((uint)(arr[offset + 3]) << 24) | ((uint)(arr[offset + 2]) << 16) | ((uint)(arr[offset + 1]) << 8) | (uint)(arr[offset]);
    }

    private int ReadInt16(in byte[] arr, uint offset)
    {
        return (arr[offset + 1] << 8) | arr[offset];
    }

    private int CalcHeaderSize(int entries)
    {
        return 4 + 4 + (8 * entries) + 4;
    }

    private int WriteDataBlockHeader(ref int data_offset, int data_size, ref List<byte> bytes)
    {
        int old_data_offset = data_offset;
        WriteInt32(data_offset, ref bytes);
        WriteInt32(data_size, ref bytes);
        data_offset += data_size + 4;
        return old_data_offset;
    }

    private void WriteDataBlock(int data_offset, in byte[] data, ref List<byte> bytes)
    {
        while (bytes.Count < data_offset)
            bytes.Add(0);

        if (bytes.Count > data_offset)
            bytes.RemoveRange(data_offset, bytes.Count - data_offset);

        bytes.AddRange(data);
        WriteInt32(0, ref bytes);
    }

    private byte[] ReadDataBlock(int block_index, string block_desc, in List<Tuple<int, int>> data_blocks, in List<byte> bytes)
    {
        int data_offset = data_blocks[block_index].Item1;
        int data_size = data_blocks[block_index].Item2;
        if (data_blocks.Count > block_index && data_size != 0)
        {
            if (data_offset + data_size > bytes.Count)
            {
                GD.Print("invalid " + block_desc + " data block");
                ui_controller.ShowErrorDialog("invalid " + block_desc + " data block");
                return null;
            }
            else
                return bytes.GetRange(data_offset, data_size).ToArray();
        }
        return null;
    }

    public void WriteSaveFile(string path)
    {
        // file format definition
        //
        // 4 byte signature - 0xCA504701
        // 2 byte header size
        // 2 byte header entry count
        // 4 byte offset of voxel data
        // 4 byte size of voxel data (0x0 if not present)
        // 4 byte offset of light data
        // 4 byte size of light data (0x0 if not present)
        // 4 byte offset of rendering config
        // 4 byte size of rendering config (0x0 if not present)
        // 4 byte offset of ivy data
        // 4 byte size of ivy data (0x0 if not present)
        // etc...
        // 4 byte padding
        // voxel data (as described by voxel data info in header table
        // ... etc

        // generate the file header
        List<byte> bytes = new List<byte>();
        WriteUInt32(0xCA504701, ref bytes);
        int header_entries = 8; // space for eight entries
        int header_size = CalcHeaderSize(header_entries);
        WriteInt16(header_size, ref bytes);
        WriteInt16(header_entries, ref bytes);

        int data_offset = header_size;

        // grab voxel data
        byte[] voxel_data = voxel_grid.SerialiseMap();
        int voxel_data_offset = WriteDataBlockHeader(ref data_offset, voxel_data.Length, ref bytes);

        // TODO: grab light data
        int light_data_offset = WriteDataBlockHeader(ref data_offset, 0, ref bytes);

        // grab rendering config
        byte[] rendering_config = ui_controller.render_options.SerialiseConfig();
        int rendering_config_offset = WriteDataBlockHeader(ref data_offset, rendering_config.Length, ref bytes);
        
        // here we would grab ivy data

        // ensure that we fill any unfilled header space
        while (bytes.Count < header_size)
            bytes.Add(0);

        // write voxel data blocks
        WriteDataBlock(voxel_data_offset, in voxel_data, ref bytes);
        // TODO: write light data block
        WriteDataBlock(rendering_config_offset, in rendering_config, ref bytes);

        Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        file.StoreBuffer(bytes.ToArray());
        file.Close();
    }

    public void ReadSaveFile(string path)
    {
        GD.Print("loading file " + path);

        Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        byte[] bytes = file.GetBuffer((long)file.GetLength());
        file.Close();

        uint signature = ReadUInt32(in bytes, 0);
        if (signature != 0xCA504701)
        {
            voxel_grid.DeserialiseMap(bytes);
            return;
        }

        int header_size = ReadInt16(in bytes, 4);
        int header_entries = ReadInt16(in bytes, 6);
        if (header_size < CalcHeaderSize(header_entries))
        {
            GD.Print("invalid savefile header size");
            ui_controller.ShowErrorDialog("invalid savefile header size");
            return;
        }

        if (header_entries == 0)
            return;

        List<Tuple<int, int>> data_blocks = new List<Tuple<int, int>>();
        uint offset = 8;
        for (int i = 0; i < header_entries; i++)
        {
            data_blocks.Add(new(ReadInt32(in bytes, offset), ReadInt32(in bytes, offset + 4)));
            offset += 8;
        }

        List<byte> bytes_as_array = new(bytes);

        // grab voxel data
        byte[] voxel_data = ReadDataBlock(0, "voxel", in data_blocks, in bytes_as_array);
        if (voxel_data != null)
            voxel_grid.DeserialiseMap(voxel_data);

        // TODO: grab light data

        // grab rendering config
        byte[] rendering_config = ReadDataBlock(2, "rendering", in data_blocks, in bytes_as_array);
        if (rendering_config != null)
            ui_controller.render_options.DeserialiseConfig(rendering_config);

        // grab ivy data
    }
}