shader_type canvas_item;

uniform sampler2D game_texture : source_color, hint_default_white, repeat_disable, filter_nearest;
uniform sampler2D ui_texture : source_color, hint_default_transparent, repeat_disable, filter_nearest;

uniform sampler2D lut : source_color, hint_default_white, filter_linear;

uniform vec3 low_colour : source_color = vec3(0.166f, 0.158f, 0.269f);
uniform vec3 high_colour : source_color = vec3(1.0f, 0.97f, 0.93f);

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

uniform float e = 1.4f;

float ease(float f, float v0, float v1)
{
	if (f <= 0.0f) return v0;
	if (f >= 1.0f) return v1;
	float d1 = abs(f);
	float d2 = abs(f - 1.0f);
	float i1 = 1.0f / pow(d1, e);
	float i2 = 1.0f / pow(d2, e);
	return ((i1 * v0) + (i2 * v1)) / (i1 + i2);
}

vec3 ease(float f, vec3 v0, vec3 v1)
{
	return vec3(ease(f, v0.x, v1.x), ease(f, v0.y, v1.y), ease(f, v0.z, v1.z));
}

vec3 sample_lut(vec3 v)
{
	float x = clamp(v.r, 0.0f, 0.99f) / 8.0f;
	float y = clamp(v.g, 0.0f, 0.99f) / 8.0f;
	float tile = floor(clamp(v.b, 0.0f, 0.99f) * 64.0f);
	x = x + (mod(tile, 8.0f) / 8.0f);
	y = y + (floor(tile / 8.0f) / 8.0f);
	return texture(lut, vec2(x, y)).rgb;
}

uniform float exposure = 1.0f;

void fragment() {
	vec2 downscaled_size = (1.0f / SCREEN_PIXEL_SIZE) / pixel_scale;
	vec2 rounded_uv = (floor(SCREEN_UV * downscaled_size) + 0.5f) / downscaled_size;

	// TODO: displace
	float game_value = texture(game_texture, rounded_uv).r * exposure;
	float overlay_value = clamp(sample_overlay(rounded_uv), 0, 1);
	float final_value = mix(game_value, game_value < 0.5f ? 1.0f : 0.0f, overlay_value);

	vec3 final_colour = (ease(clamp(final_value, 0, 1), (low_colour), (high_colour)));//mix(low_colour, high_colour, clamp(ease(final_value), 0.0f, 1.0f));

	// convert to filmic, medium high contrast (a la blender)
	COLOR.rgb = sample_lut(final_colour);
}