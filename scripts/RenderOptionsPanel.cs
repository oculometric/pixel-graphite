using Godot;
using System;

public partial class RenderOptionsPanel : Control
{
    [Export] private ShaderMaterial material;

    [Export] private Label pixels_label;
    private int pixels = 2;
    [Export] private Button pixels_down_but;
    [Export] private Button pixels_up_but;

    [Export] private Label contrast_label;
    private float contrast = 0.95f;
    [Export] private HSlider contrast_slider;

    [Export] private Label edge_label;
    private float edge = 0.151f;
    [Export] private HSlider edge_slider;

    [Export] private Label noise_label;
    private float noise = 0.02f;
    [Export] private HSlider noise_slider;

    [Export] private Label sketch_label;
    private float sketch = 0.041f;
    [Export] private HSlider sketch_slider;

    [Export] private Label posterise_label;
    private int posterise_index = 0;
    private int[] posterise_options = [2, 4, 8, 16, 24, 32, 48, 64, 72, 96, 128, 256, 384, 512];
    [Export] private Button posterise_next_but;
    [Export] private Button posterise_back_but;

    [Export] private ColorPicker picker;
    private int pal_picking = 0;
    [Export] private Label pal_high_label;
    private Color pal_high;
    [Export] private Button pal_high_but;
    [Export] private Label pal_low_label;
    private Color pal_low;
    [Export] private Button pal_low_but;
    [Export] private Label pal_mid_label;
    private Color pal_mid;
    [Export] private Button pal_mid_but;

    [Export] private Button background_but;
    private bool is_high = true;

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

        contrast_slider.ValueChanged += (double new_val) =>
        {
            contrast = (float)new_val;
            UpdateShader();
        };

        edge_slider.ValueChanged += (double new_val) =>
        {
            edge = (float)new_val;
            UpdateShader();
        };

        noise_slider.ValueChanged += (double new_val) =>
        {
            noise = (float)new_val;
            UpdateShader();
        };

        sketch_slider.ValueChanged += (double new_val) =>
        {
            sketch = (float)new_val;
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

        pal_high_but.Pressed += () =>
        {
            picker.Color = pal_high;
            pal_picking = 0;
            picker.GetWindow().Visible = true;
        };
        pal_low_but.Pressed += () =>
        {
            picker.Color = pal_low;
            pal_picking = 1;
            picker.GetWindow().Visible = true;
        };
        pal_mid_but.Pressed += () =>
        {
            picker.Color = pal_mid;
            pal_picking = 2;
            picker.GetWindow().Visible = true;
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
            contrast_slider.Value = contrast;
            edge_slider.Value = edge;
            noise_slider.Value = noise;
            sketch_slider.Value = sketch;
            UpdateLabels();
        };
    }

    private void UpdateLabels()
    {
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
    }

    private void UpdateShader()
    {
        UpdateLabels();

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
    }
}
