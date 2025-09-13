using Godot;


public partial class EditController : Node3D
{
    [Export] public string ui_name = "untitled";
    [Export(PropertyHint.MultilineText)] public string ui_controls = "...";
    [Export] public Texture2D ui_icon;
    [Export] public MainSceneController scene_controller;
    public virtual void SetEditingEnabled(bool enabled) { }
}