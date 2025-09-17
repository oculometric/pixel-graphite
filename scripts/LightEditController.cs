using Godot;
using System;
using System.Collections.Generic;

public partial class LightEditController : EditController
{
    [Export] private Light3D sun;
    [Export] private float granular_pan_speed = 4.0f;
    [Export] private float snap_angle = 45.0f;
    
    private Basis default_basis;
    public SaveManager save_manager;

    public override void _Ready()
    {
        default_basis = sun.GlobalBasis;
    }

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.Shift) && IsProcessingUnhandledInput())
        {
            if (Input.IsKeyPressed(Key.Ctrl))
                return;
            float rot_y = ((Input.IsKeyPressed(Key.D) ? 1 : 0) - (Input.IsKeyPressed(Key.A) ? 1 : 0)) * (float)delta * granular_pan_speed;
            float rot_x = ((Input.IsKeyPressed(Key.S) ? 1 : 0) - (Input.IsKeyPressed(Key.W) ? 1 : 0)) * (float)delta * granular_pan_speed;
            RotateUpAxis(rot_y);
            RotateRightAxis(rot_x);
            if (rot_y != 0.0f || rot_x != 0.0f)
                save_manager.SetUnsavedFlag();
        }
    }

    private void RotateUpAxis(float angle)
    {
        sun.RotateY(Mathf.DegToRad(angle));
    }

    private void RotateRightAxis(float angle)
    {
        sun.Rotate(sun.GlobalBasis.Column0, Mathf.DegToRad(angle));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            if (Input.IsKeyPressed(Key.Ctrl))
                return;
            InputEventKey key = @event as InputEventKey;
            if (!Input.IsKeyPressed(Key.Shift))
            {
                if (key.IsReleased())
                {
                    switch (key.Keycode)
                    {
                        case Key.A: RotateUpAxis(-snap_angle); save_manager.SetUnsavedFlag(); break;
                        case Key.D: RotateUpAxis(snap_angle); save_manager.SetUnsavedFlag(); break;
                        case Key.W: RotateRightAxis(-snap_angle); save_manager.SetUnsavedFlag(); break;
                        case Key.S: RotateRightAxis(snap_angle); save_manager.SetUnsavedFlag(); break;
                    }
                }
            }
            
            if (key.IsPressed())
            {
                switch (key.Keycode)
                {
                    case Key.Q: sun.Visible = !sun.Visible; save_manager.SetUnsavedFlag(); break;
                    case Key.E: GetWorld3D().Environment.AmbientLightEnergy = GetWorld3D().Environment.AmbientLightEnergy > 0.0f ? 0.0f : 1.0f; save_manager.SetUnsavedFlag(); break;
                    case Key.R: sun.ShadowEnabled = !sun.ShadowEnabled; save_manager.SetUnsavedFlag(); break;
                }
            }
        }
    }

    public byte[] SerialiseData()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add((byte)'L');
        bytes.Add((byte)'D');
        bytes.Add((byte)'P');
        bytes.Add((byte)'G');

        SaveHelpers.WriteFloat(sun.GlobalBasis.Column0.X, ref bytes);
        SaveHelpers.WriteFloat(sun.GlobalBasis.Column0.Y, ref bytes);
        SaveHelpers.WriteFloat(sun.GlobalBasis.Column0.Z, ref bytes);
        SaveHelpers.WriteFloat(sun.GlobalBasis.Column1.X, ref bytes);
        SaveHelpers.WriteFloat(sun.GlobalBasis.Column1.Y, ref bytes);
        SaveHelpers.WriteFloat(sun.GlobalBasis.Column1.Z, ref bytes);
        SaveHelpers.WriteFloat(sun.GlobalBasis.Column2.X, ref bytes);
        SaveHelpers.WriteFloat(sun.GlobalBasis.Column2.Y, ref bytes);
        SaveHelpers.WriteFloat(sun.GlobalBasis.Column2.Z, ref bytes);
        bytes.Add((byte)((sun.Visible ? 0b1 : 0) | (sun.ShadowEnabled ? 0b10 : 0) | ((GetWorld3D().Environment.AmbientLightEnergy > 0.0f) ? 0b100 : 0)));

        return bytes.ToArray();
    }

    public void DeserialiseData(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 41 || !(bytes[0] == 'L' && bytes[1] == 'D' && bytes[2] == 'P' && bytes[3] == 'G'))
        {
            sun.GlobalBasis = default_basis;
            sun.Visible = true;
            sun.ShadowEnabled = true;
            GetWorld3D().Environment.AmbientLightEnergy = 1.0f;
            return;
        }

        Vector3 col0 = new(SaveHelpers.ReadFloat(in bytes, 4), SaveHelpers.ReadFloat(in bytes, 8), SaveHelpers.ReadFloat(in bytes, 12));
        Vector3 col1 = new(SaveHelpers.ReadFloat(in bytes, 16), SaveHelpers.ReadFloat(in bytes, 20), SaveHelpers.ReadFloat(in bytes, 24));
        Vector3 col2 = new(SaveHelpers.ReadFloat(in bytes, 28), SaveHelpers.ReadFloat(in bytes, 32), SaveHelpers.ReadFloat(in bytes, 36));
        sun.GlobalBasis = new(col0, col1, col2);
        byte flags = bytes[40];
        sun.Visible = (flags & 0b1) > 0;
        sun.ShadowEnabled = (flags & 0b10) > 0;
        GetWorld3D().Environment.AmbientLightEnergy = ((flags & 0b100) > 0) ? 1.0f : 0.0f;
    }
}
