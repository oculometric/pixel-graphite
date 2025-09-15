using Godot;

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
            voxel_grid.Save(autosave_name);
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
        voxel_grid.Save(file);
        has_been_saved = true;
        current_file_name = file;
        has_unsaved_changes = false;
        autosave_timer.Start(autosave_time);
        UpdateTitle();
    }

    public void LoadData(string file)
    {
        voxel_grid.Load(file);
        has_been_saved = true;
        current_file_name = file;
        has_unsaved_changes = false;
        UpdateTitle();
    }
}