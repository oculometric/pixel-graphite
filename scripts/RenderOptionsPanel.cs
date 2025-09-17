using Godot;
using System;
using System.Collections.Generic;

public partial class RenderOptionsPanel : Control
{
    [Export] private ShaderMaterial material;

    [Export] private Label pixels_label;
    private int pixels = 2;
    private int pixels_default = 2;
    [Export] private Button pixels_down_but;
    [Export] private Button pixels_up_but;
    [Export] private Button pixels_reset;

    [Export] private Label contrast_label;
    private float contrast = 0.95f;
    private float contrast_default = 0.95f;
    [Export] private HSlider contrast_slider;
    [Export] private Button contrast_reset;

    [Export] private Label edge_label;
    private float edge = 0.151f;
    private float edge_default = 0.151f;
    [Export] private HSlider edge_slider;
    [Export] private Button edge_reset;

    [Export] private Label noise_label;
    private float noise = 0.02f;
    private float noise_default = 0.02f;
    [Export] private HSlider noise_slider;
    [Export] private Button noise_reset;

    [Export] private Label sketch_label;
    private float sketch = 0.041f;
    private float sketch_default = 0.041f;
    [Export] private HSlider sketch_slider;
    [Export] private Button sketch_reset;

    [Export] private Label posterise_label;
    private int posterise_index = 0;
    private int posterise_default = 6;
    private int[] posterise_options = [2, 4, 8, 16, 24, 32, 48, 64, 72, 96, 128, 256, 384, 512];
    [Export] private Button posterise_next_but;
    [Export] private Button posterise_back_but;
    [Export] private Button posterise_reset;

    [Export] private ColorPicker picker;
    private int pal_picking = 0;
    [Export] private Label pal_high_label;
    private Color pal_high;
    private Color pal_high_default = new Color(2.0f, 1.073f, 0.797f);
    [Export] private Button pal_high_but;
    [Export] private Button pal_high_reset;
    [Export] private Label pal_low_label;
    private Color pal_low;
    private Color pal_low_default = new Color(0.015f, 0.021f, 0.004f);
    [Export] private Button pal_low_but;
    [Export] private Button pal_low_reset;
    [Export] private Label pal_mid_label;
    private Color pal_mid;
    private Color pal_mid_default = new Color(0.509f, 0.261f, 0.199f);
    [Export] private Button pal_mid_but;
    [Export] private Button pal_mid_reset;
    [Export] private Button pal_tri_but;
    private bool three_col = true;
    private bool three_col_default = true;
    [Export] private Button pal_tri_reset;

    [Export] private Button background_but;
    private bool is_high = true;
    private bool is_high_default = true;
    [Export] private Button background_reset;

    [Export] private Button displace_but;
    private bool displace = true;
    private bool displace_default = true;
    [Export] private Button displace_reset;

    [Export] private Button save_palette_but;
    [Export] private Button load_palette_but;
    [Export] private FileDialog load_save_dialog;

    public SaveManager save_manager;

    public override void _Ready()
    {
        pixels_down_but.Pressed += () =>
        {
            pixels = int.Max(pixels - 1, 1);
            UpdateShader();
        };
        pixels_up_but.Pressed += () =>
        {
            pixels = int.Min(pixels + 1, 256);
            UpdateShader();
        };
        pixels_reset.Pressed += () =>
        {
            pixels = pixels_default;
            UpdateShader();
        };

        contrast_slider.ValueChanged += (double new_val) =>
        {
            contrast = (float)new_val;
            UpdateShader();
        };
        contrast_reset.Pressed += () =>
        {
            contrast = contrast_default;
            UpdateShader();
        };

        edge_slider.ValueChanged += (double new_val) =>
        {
            edge = (float)new_val;
            UpdateShader();
        };
        edge_reset.Pressed += () =>
        {
            edge = edge_default;
            UpdateShader();
        };

        noise_slider.ValueChanged += (double new_val) =>
        {
            noise = (float)new_val;
            UpdateShader();
        };
        noise_reset.Pressed += () =>
        {
            noise = noise_default;
            UpdateShader();
        };

        sketch_slider.ValueChanged += (double new_val) =>
        {
            sketch = (float)new_val;
            UpdateShader();
        };
        sketch_reset.Pressed += () =>
        {
            sketch = sketch_default;
            UpdateShader();
        };

        posterise_back_but.Pressed += () =>
        {
            posterise_index = int.Max(posterise_index - 1, 0);
            UpdateShader();
        };
        posterise_next_but.Pressed += () =>
        {
            posterise_index = int.Min(posterise_index + 1, posterise_options.Length - 1);
            UpdateShader();
        };
        posterise_reset.Pressed += () =>
        {
            posterise_index = posterise_default;
            UpdateShader();
        };

        pal_high_but.Pressed += () =>
        {
            picker.Color = pal_high;
            pal_picking = 0;
            picker.GetWindow().Visible = true;
        };
        pal_high_reset.Pressed += () =>
        {
            pal_high = pal_high_default;
            UpdateShader();
        };
        pal_low_but.Pressed += () =>
        {
            picker.Color = pal_low;
            pal_picking = 1;
            picker.GetWindow().Visible = true;
        };
        pal_low_reset.Pressed += () =>
        {
            pal_low = pal_low_default;
            UpdateShader();
        };
        pal_mid_but.Pressed += () =>
        {
            picker.Color = pal_mid;
            pal_picking = 2;
            picker.GetWindow().Visible = true;
        };
        pal_mid_reset.Pressed += () =>
        {
            pal_mid = pal_mid_default;
            UpdateShader();
        };
        pal_tri_but.Pressed += () =>
        {
            three_col = pal_tri_but.ButtonPressed;
            UpdateShader();
        };
        pal_tri_reset.Pressed += () =>
        {
            three_col = three_col_default;
            UpdateShader();
        };
        picker.ColorChanged += (Color new_col) =>
        {
            if (pal_picking == 0)
                pal_high = new_col;
            else if (pal_picking == 1)
                pal_low = new_col;
            else if (pal_picking == 2)
                pal_mid = new_col;
            UpdateShader();
        };

        background_but.Pressed += () =>
        {
            is_high = !is_high;
            UpdateShader();
        };
        background_reset.Pressed += () =>
        {
            is_high = is_high_default;
            UpdateShader();
        };

        displace_but.Pressed += () =>
        {
            displace = displace_but.ButtonPressed;
            UpdateShader();
        };
        displace_reset.Pressed += () =>
        {
            displace = displace_default;
            UpdateShader();
        };

        save_palette_but.Pressed += SavePalette;
        load_palette_but.Pressed += LoadPalette;
        load_save_dialog.FileSelected += (string path) =>
        {
            if (load_save_dialog.FileMode == FileDialog.FileModeEnum.SaveFile)
            {
                Image img = Image.CreateEmpty(three_col ? 3 : 2, 1, false, Image.Format.Rgbf);
                img.SetPixel(0, 0, pal_high);
                img.SetPixel(1, 0, pal_low);
                if (three_col)
                    img.SetPixel(2, 0, pal_mid);
                img.SaveExr(load_save_dialog.CurrentPath);
            }
            else
            {
                Image img = new Image();
                img.Load(load_save_dialog.CurrentPath);

                if (img.GetSize().Y != 1)
                    return;
                if (img.GetSize().X < 2)
                    return;
                if (img.GetSize().X > 3)
                    return;

                pal_high = img.GetPixel(0, 0);
                pal_low = img.GetPixel(1, 0);
                if (img.GetSize().X == 2)
                {
                    three_col = false;
                    pal_tri_but.ButtonPressed = three_col;
                }
                else
                {
                    three_col = true;
                    pal_tri_but.ButtonPressed = three_col;
                    pal_mid = img.GetPixel(2, 0);
                }
                UpdateShader();
            }
        };

        VisibilityChanged += () =>
        {
            if (!Visible)
                return;
            pixels = material.GetShaderParameter("pixelation_size").AsInt32();
            contrast = (float)material.GetShaderParameter("contrast").AsDouble();
            edge = (float)material.GetShaderParameter("edge_colour_multiplier").AsDouble();
            noise = (float)material.GetShaderParameter("noise_factor").AsDouble();
            sketch = (float)material.GetShaderParameter("sketch_factor").AsDouble();
            int steps = material.GetShaderParameter("posterise_steps").AsInt32();
            int best = 1000000;
            for (int i = 0; i < posterise_options.Length; i++)
            {
                int diff = int.Abs(steps - posterise_options[i]);
                if (diff < best)
                {
                    best = diff;
                    posterise_index = i;
                }
            }
            is_high = material.GetShaderParameter("background_value").AsDouble() > 0.5f;
            pal_high = material.GetShaderParameter("palette_high").AsColor();
            pal_low = material.GetShaderParameter("palette_low").AsColor();
            pal_mid = material.GetShaderParameter("palette_mid").AsColor();
            three_col = material.GetShaderParameter("use_palette_mid").AsBool();
            displace = material.GetShaderParameter("enable_displacement").AsBool();
            
            UpdateLabels();
        };
    }

    private void UpdateLabels()
    {
        contrast_slider.Value = contrast;
        edge_slider.Value = edge;
        noise_slider.Value = noise;
        sketch_slider.Value = sketch;
        pal_tri_but.ButtonPressed = three_col;
        displace_but.ButtonPressed = displace;
        pixels_label.Text = pixels.ToString();
        contrast_label.Text = contrast.ToString("0.00");
        edge_label.Text = edge.ToString("0.00");
        noise_label.Text = noise.ToString("0.00");
        sketch_label.Text = sketch.ToString("0.00");
        posterise_label.Text = posterise_options[posterise_index].ToString();
        background_but.Text = is_high ? "high" : "low";
        pal_high_label.Text = string.Format("R {0:F3}; G {1:F3}; B {2:F3};", pal_high.R, pal_high.G, pal_high.B);
        pal_low_label.Text = string.Format("R {0:F3}; G {1:F3}; B {2:F3};", pal_low.R, pal_low.G, pal_low.B);
        pal_mid_label.Text = string.Format("R {0:F3}; G {1:F3}; B {2:F3};", pal_mid.R, pal_mid.G, pal_mid.B);
        pal_tri_but.Text = three_col ? "enabled" : "disabled";
        displace_but.Text = displace ? "enabled" : "disabled";
    }

    private void UpdateShader(bool set_unsaved_flag = true)
    {
        UpdateLabels();

        if (set_unsaved_flag && save_manager != null)
            save_manager.SetUnsavedFlag();

        material.SetShaderParameter("pixelation_size", pixels);
        material.SetShaderParameter("contrast", contrast);
        material.SetShaderParameter("edge_colour_multiplier", edge);
        material.SetShaderParameter("noise_factor", noise);
        material.SetShaderParameter("sketch_factor", sketch);
        material.SetShaderParameter("posterise_steps", posterise_options[posterise_index]);
        material.SetShaderParameter("background_value", is_high ? 1.0f : 0.0f);
        material.SetShaderParameter("palette_high", pal_high);
        material.SetShaderParameter("palette_low", pal_low);
        material.SetShaderParameter("palette_mid", pal_mid);
        material.SetShaderParameter("use_palette_mid", three_col);
        material.SetShaderParameter("enable_displacement", displace);
    }

    public void SavePalette()
    {
        load_save_dialog.Title = "save palette";
        load_save_dialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        load_save_dialog.OkButtonText = "save";
        load_save_dialog.Visible = true;
    }

    public void LoadPalette()
    {
        load_save_dialog.Title = "load palette";
        load_save_dialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        load_save_dialog.OkButtonText = "load";
        load_save_dialog.Visible = true;
    }

    public byte[] SerialiseConfig()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add((byte)'R');
        bytes.Add((byte)'C');
        bytes.Add((byte)'P');
        bytes.Add((byte)'G');

        SaveHelpers.WriteInt32(pixels, ref bytes);
        SaveHelpers.WriteFloat(contrast, ref bytes);
        SaveHelpers.WriteFloat(edge, ref bytes);
        SaveHelpers.WriteFloat(noise, ref bytes);
        SaveHelpers.WriteFloat(sketch, ref bytes);
        SaveHelpers.WriteInt16(posterise_index, ref bytes);
        SaveHelpers.WriteFloat(pal_high.R, ref bytes);
        SaveHelpers.WriteFloat(pal_high.G, ref bytes);
        SaveHelpers.WriteFloat(pal_high.B, ref bytes);
        SaveHelpers.WriteFloat(pal_low.R, ref bytes);
        SaveHelpers.WriteFloat(pal_low.G, ref bytes);
        SaveHelpers.WriteFloat(pal_low.B, ref bytes);
        SaveHelpers.WriteFloat(pal_mid.R, ref bytes);
        SaveHelpers.WriteFloat(pal_mid.G, ref bytes);
        SaveHelpers.WriteFloat(pal_mid.B, ref bytes);
        bytes.Add(three_col ? (byte)1 : (byte)0);
        bytes.Add(is_high ? (byte)1 : (byte)0);
        bytes.Add(displace ? (byte)1 : (byte)0);

        return bytes.ToArray();
    }

    public void DeserialiseConfig(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 65 || !(bytes[0] == 'R' && bytes[1] == 'C' && bytes[2] == 'P' && bytes[3] == 'G'))
        {
            pixels = pixels_default;
            contrast = contrast_default;
            edge = edge_default;
            noise = noise_default;
            sketch = sketch_default;
            posterise_index = posterise_default;
            pal_high = pal_high_default;
            pal_low = pal_low_default;
            pal_mid = pal_mid_default;
            three_col = three_col_default;
            is_high = is_high_default;
            displace = displace_default;
            UpdateShader(false);
            return;
        }

        pixels = SaveHelpers.ReadInt32(in bytes, 4);
        contrast = SaveHelpers.ReadFloat(in bytes, 8);
        edge = SaveHelpers.ReadFloat(in bytes, 12);
        noise = SaveHelpers.ReadFloat(in bytes, 16);
        sketch = SaveHelpers.ReadFloat(in bytes, 20);
        posterise_index = SaveHelpers.ReadInt16(in bytes, 24);
        pal_high = new(SaveHelpers.ReadFloat(in bytes, 26), SaveHelpers.ReadFloat(in bytes, 30), SaveHelpers.ReadFloat(in bytes, 34));
        pal_low = new(SaveHelpers.ReadFloat(in bytes, 38), SaveHelpers.ReadFloat(in bytes, 42), SaveHelpers.ReadFloat(in bytes, 46));
        pal_mid = new(SaveHelpers.ReadFloat(in bytes, 50), SaveHelpers.ReadFloat(in bytes, 54), SaveHelpers.ReadFloat(in bytes, 58));
        three_col = bytes[62] > 0;
        is_high = bytes[63] > 0;
        displace = bytes[64] > 0;

        UpdateShader(false);
    }
}
