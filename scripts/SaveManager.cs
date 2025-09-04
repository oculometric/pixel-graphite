using Godot;

public partial class SaveManager : Node3D
{
    [Export] public VoxelGrid voxel_grid { get; private set; }
    [Export] public EditingUIController ui_controller { get; private set; }
    [Export] public float autosave_time = 10.0f;

    private string current_file_name = "Untitled";
    private bool has_been_saved = false;
    private bool has_unsaved_changes = true;
    private Timer autosave_timer = null;

    public void SetUnsavedFlag() { has_unsaved_changes = true; }

    public override void _Ready()
    {
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

            string current_file_tmp = current_file_name;
            bool has_unsaved_tmp = has_unsaved_changes;
            string file_name_base = current_file_name.GetFile().GetBaseName();
            string autosave_name = "user://autosave/" + file_name_base + "_autosave.dat";
            Godot.DirAccess.Open("user://").MakeDir("autosave");
            SaveData(autosave_name);
            current_file_name = current_file_tmp;
            has_unsaved_changes = has_unsaved_tmp;
        };
        autosave_timer.Start(autosave_time);
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
                {
                    if (has_been_saved && !key.ShiftPressed)
                        SaveData(current_file_name);
                    else
                        ui_controller.ShowSaveDialog(current_file_name);
                }
                else if (key.Keycode == Key.O)
                {
                    if (has_unsaved_changes)
                        ui_controller.ShowConfirmDialog("discard unsaved changes?", "you have made changes which are not saved. are you sure you want to discard them and open another file?", "discard changes", "cancel", ConfirmDiscard);
                    else
                        ui_controller.ShowLoadDialog(current_file_name);
                }
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
    }

    public void LoadData(string file)
    {
        voxel_grid.Load(file);
        has_been_saved = true;
        current_file_name = file;
        has_unsaved_changes = false;
    }
}