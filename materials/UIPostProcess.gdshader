shader_type canvas_item;

uniform sampler2D game_texture : source_color, hint_default_white, repeat_disable, filter_nearest;
uniform sampler2D ui_texture : source_color, hint_default_transparent, repeat_disable, filter_nearest;

uniform sampler2D lut : hint_default_white, filter_linear;

uniform float pixel_scale = 1.0f;

vec3 srgb_to_linear(vec3 v)
{
	return mix(v / 12.92, pow((v + 0.055f) / 1.055f, vec3(2.4f)), vec3(greaterThan(v, vec3(0.04045f))));
}

vec3 linear_to_srgb(vec3 v)
{
	return mix(v * 12.92, (pow(v, vec3(1.0f / 2.4f)) * 1.055f) - 0.055f, vec3(greaterThan(v, vec3(0.0031308f))));
}

void vertex()
{
}

float sample_overlay(vec2 uv)
{
	vec4 sample = texture(ui_texture, uv);
	return float(length(sample.rgb) > 0.01f && sample.a > 0.25f);
}

vec3 sample_lut(float f)
{
	float f_2 = clamp(f, 0.0f, 1.0f);
	float x = mod(f_2 * 64.0f, 1.0f);
	float y = 1.0f - (floor(f_2 * 64.0f) / 64.0f);
	return texture(lut, vec2(x, y)).rgb;
}

void fragment() {
	vec2 downscaled_size = (1.0f / SCREEN_PIXEL_SIZE) / pixel_scale;
	vec2 rounded_uv = (floor(SCREEN_UV * downscaled_size) + 0.5f) / downscaled_size;

	// TODO: displace
	float game_value = texture(game_texture, rounded_uv).r;
	float overlay_value = clamp(sample_overlay(rounded_uv), 0, 1);
	float final_value = mix(game_value, game_value < 0.5f ? 1.0f : 0.0f, overlay_value);

	// convert to filmic, medium high contrast (a la blender)
	//COLOR.rgb = vec3(final_value);
	COLOR.rgb = sample_lut(final_value);
}