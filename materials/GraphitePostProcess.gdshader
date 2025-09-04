shader_type spatial;
render_mode unshaded, cull_back, fog_disabled;

#include "Fbm.gdshaderinc"

uniform sampler2D screen_tex : source_color, filter_nearest, hint_screen_texture, repeat_disable;
uniform sampler2D normal_tex : filter_nearest, hint_normal_roughness_texture, repeat_disable;
uniform sampler2D depth_tex : filter_nearest, hint_depth_texture, repeat_disable;
uniform sampler2D overlay_tex : filter_nearest, hint_default_transparent, repeat_disable;

uniform int pixelation_size = 2;
uniform float contrast = 0.95f;
uniform float edge_colour_multiplier = 0.151f;
uniform float noise_sketch_scale = 1024.0f;
uniform float noise_factor = 0.02f;
uniform float sketch_factor = 0.061f;
uniform float posterise_steps = 64.0f;
uniform vec3 palette_low : source_color = vec3(0.015, 0.021, 0.004);
uniform vec3 palette_high : source_color = vec3(2.000, 1.073, 0.797);
uniform bool use_palette_mid = false;
uniform vec3 palette_mid : source_color = vec3(0.509, 0.261, 0.199);

uniform sampler3D lut : hint_default_white, filter_linear, repeat_disable;

void vertex()
{
	POSITION = vec4(VERTEX.xy, 1, 1);
}

vec3 srgb_to_linear(vec3 v)
{
	return mix(v / 12.92, pow((v + 0.055f) / 1.055f, vec3(2.4f)), vec3(greaterThan(v, vec3(0.04045f))));
}

vec3 linear_to_srgb(vec3 v)
{
	return mix(v * 12.92, (pow(v, vec3(1.0f / 2.4f)) * 1.055f) - 0.055f, vec3(greaterThan(v, vec3(0.0031308f))));
}

float linear_depth(vec2 uv, mat4 inv_proj)
{
	float raw_depth = texture(depth_tex, uv).x;
	vec3 ndc = vec3(uv * 2.0 - 1.0, raw_depth);
	vec4 view_position = inv_proj * vec4(ndc, 1.0);
	float depth_sample = -view_position.z / view_position.w;

	return depth_sample;
}

vec3 sample_normal(vec2 uv)
{
	return (texture(normal_tex, uv).xyz * 2.0f) - 1.0f;
}

float edge_mask(vec2 uv, vec2 pixel_step, mat4 inv_proj)
{
	vec2 u0 = (uv);
	vec2 u1 = (uv + (pixel_step * vec2(-1, -1)));
	vec2 u2 = (uv + (pixel_step * vec2(1, 1)));
	vec2 u3 = (uv + (pixel_step * vec2(1, -1)));
	vec2 u4 = (uv + (pixel_step * vec2(-1, 1)));

	vec3 normal_sharpen = (sample_normal(u0) * 5.0f)
						- (sample_normal(u1)
						+ sample_normal(u2)
						+ sample_normal(u3)
						+ sample_normal(u4));

	float ld = linear_depth(u0, inv_proj);
	float depth_sharpen = (ld * 5.0f)
						- (linear_depth(u1, inv_proj)
						+ linear_depth(u2, inv_proj)
						+ linear_depth(u3,inv_proj)
						+ linear_depth(u4, inv_proj));

	float normal_value = dot(abs(normal_sharpen), vec3(0.333f, 0.333f, 0.333f));
	float depth_value = clamp(abs(depth_sharpen) - ld, 0.0f, 1.0f);
	float final_value = smoothstep(0.571f, 0.873f, normal_value + depth_value);
	return final_value;
}

float sketch_partial(vec2 uv, vec2 scale, vec2 pattern_scale)
{
	vec2 co = uv * scale * pattern_scale;
	float across = co.x - co.y;
	float c = float(((cos(across * PI * 2.0f * PI) * -0.5f) + 0.5f) > 0.3f);
	return c - 0.5f;
}

float sketch(vec2 uv, vec2 scale, float screen_value)
{
	float s1 = sketch_partial(uv, scale, vec2(-450.0f, 250.0f));
	float s2 = sketch_partial(uv, scale, vec2(-500.0f, 250.0f));
	float m = (s1 + s2) * sqrt(screen_value) * sketch_factor;
	return clamp(screen_value + m, 0.0f, 1.0f);
}

float overlay(float scene_value, vec2 pixel_uv)
{
	vec4 overlay_pix = texture(overlay_tex, pixel_uv);
	float overlay_value = (clamp(overlay_pix.r + overlay_pix.g + overlay_pix.b, 0.0f, 1.0f) * overlay_pix.a * 2.0f);
	if (overlay_value > 0.5f)
		return float(scene_value < 0.5f);
	else
		return scene_value;
}

vec3 colourise(float value)
{
	float final_value = clamp(value, 0.0f, 1.0f);
	vec3 final_colour = vec3(0.0f);
	if (use_palette_mid)
	{
		if (final_value > 0.5f)
			final_colour = mix(linear_to_srgb(palette_mid), linear_to_srgb(palette_high), smoothstep(0.0f, 1.0f, (final_value * 2.0f) - 1.0f));
		else
			final_colour = mix(linear_to_srgb(palette_low), linear_to_srgb(palette_mid), smoothstep(0.0f, 1.0f, (final_value * 2.0f)));
	}
	else
		final_colour = mix(linear_to_srgb(palette_low), linear_to_srgb(palette_high), smoothstep(0.0f, 1.0f, final_value));
	return final_colour;
}

vec3 tonemap(vec3 col)
{
	return srgb_to_linear(texture(lut, col.rgb * 0.5f).rgb);
}

void fragment()
{
	// compute the whole scene at standard scale, but DONT compute edge values for both pixels and then downscale (average) them
	// essentially only compute the top left corner of each block of 2x2, but use the whole buffer still
	// output into only the top quarter of the buffer (discard the rest based on uv)
	// then scale that output to fill the screen in the viewport (only 2 copies, UI into this, then this onto screen)

	vec2 downscaled_viewport_size = VIEWPORT_SIZE / float(pixelation_size);
	vec2 downscaled_uv = floor(SCREEN_UV * downscaled_viewport_size/* * float(pixelation_size)*/) / downscaled_viewport_size;
	/*if (downscaled_uv.x > 1.0f || downscaled_uv.y > 1.0f)
		discard;*/ // for a slight performance boost, this (and the code above) can be uncommented

	vec3 screen_sample = texture(screen_tex, downscaled_uv).rgb;
	float screen_value = dot(screen_sample, vec3(0.2126f, 0.7151f, 0.0722f));
	screen_value = 1.0f - ((1.0f - screen_value) * contrast);

	// TODO: reimplement edge displacement for all UVs (including depth, edge)

	// compute edge mask via sharpen filters
	float edges = edge_mask(downscaled_uv, 1.0f / (VIEWPORT_SIZE * float(pixelation_size)), INV_PROJECTION_MATRIX);
	float edged_value = mix(screen_value, screen_value * edge_colour_multiplier, edges);

	// apply grain
	vec2 scaled_viewport = VIEWPORT_SIZE / noise_sketch_scale;
	float noise = ((fbm((downscaled_uv / 0.005f) * 20.0f * scaled_viewport, 2, 2.0f, 0.5f)) * 1.2f) - 0.7f;
	float noised_value = edged_value + (floor(noise * 9.0f) * noise_factor / 9.0f);
	// posterise
	float posterised_value = round(noised_value * posterise_steps) / posterise_steps;
	// add sketch effect
	float sketched_value = sketch(downscaled_uv, scaled_viewport, posterised_value);

	// apply solid background
	if (texture(depth_tex, downscaled_uv).r == 0.0f)
		sketched_value = 1.0f;

	// apply overlay texture
	float overlaid_value = overlay(sketched_value, downscaled_uv);

	// colourise
	vec3 unmapped_colour = colourise(overlaid_value);
	// tonemap
	ALBEDO = tonemap(unmapped_colour);
}
