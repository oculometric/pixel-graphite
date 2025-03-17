shader_type canvas_item;

uniform sampler2D ui_texture : source_color, hint_default_transparent, repeat_disable, filter_nearest;

uniform float pixel_scale = 1.0f;

void vertex()
{
}

float sample_overlay(vec2 uv)
{
	vec4 sample = texture(ui_texture, uv);
	return float(length(sample.rgb) > 0.01f && sample.a > 0.0f);
}

float bloom(vec2 center, vec2 pixel_size, int steps, float weight)
{
	float center_sample = sample_overlay(center);
	float sum = center_sample * weight;
	vec2 uv = center - (pixel_size * float(steps));
	float uv_y_start = uv.y;
	for (int i = -steps; i <= steps; i++)
	{
		for (int j = -steps; j <= steps; j++)
		{
			float dist = 1.0f / ((1.0f * sqrt(float((i * i) + (j * j)))) + 1.0f);
			sum += sample_overlay(uv) * dist;
			uv.y += pixel_size.y;
		}
		uv.y = uv_y_start;
		uv.x += pixel_size.x;
	}
	return (sum / ((weight + 1.0f) * float(steps))) + center_sample;
}

void fragment() {
	vec2 downscaled_size = (1.0f / SCREEN_PIXEL_SIZE) / pixel_scale;
	vec2 rounded_uv = (floor(SCREEN_UV * downscaled_size) + 0.5f) / downscaled_size;

	//float original_pixel = pow(bloom(rounded_uv, 1.0f / downscaled_size, 8, 2.5f), 1.9f);
	float original_pixel = sample_overlay(rounded_uv);
	COLOR.rgb = vec3(0.0f);
	COLOR.a = float(original_pixel > 0.5f);
}