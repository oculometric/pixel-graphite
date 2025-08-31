shader_type spatial;
render_mode unshaded, cull_back, fog_disabled;

#include "Fbm.gdshaderinc"

uniform sampler2D screen_tex : source_color, filter_nearest, hint_screen_texture, repeat_disable;
uniform sampler2D normal_tex : filter_nearest, hint_normal_roughness_texture, repeat_disable;
uniform sampler2D depth_tex : filter_nearest, hint_depth_texture, repeat_disable;
uniform sampler2D overlay_tex : filter_nearest, hint_default_transparent, repeat_disable;

uniform int pixelation_size = 2;
uniform vec3 palette_low : source_color = vec3(0.015208, 0.011115, 0.003702);
uniform vec3 palette_high : source_color = vec3(2.000000, 1.073330, 0.797370);
uniform bool use_palette_mid = false;
uniform vec3 palette_mid : source_color = vec3(0.513966, 0.272809, 0.186906);

uniform sampler2D lut : hint_default_white, filter_linear, repeat_disable;

void vertex()
{
	POSITION = vec4(VERTEX.xy, 1, 1);
}

float linear_depth(vec2 uv, mat4 inv_proj)
{
	float raw_depth = texture(depth_tex, uv).x;
	vec3 ndc = vec3(uv * 2.0 - 1.0, raw_depth);
	vec4 view_position = inv_proj * vec4(ndc, 1.0);
	float depth_sample = -view_position.z / view_position.w;

	return depth_sample;
}

float edge_mask(vec2 uv, vec2 pixel_step, mat4 inv_proj)
{
	vec2 u0 = (uv);
	vec2 u1 = (uv + (pixel_step * vec2(-1, -1)));
	vec2 u2 = (uv + (pixel_step * vec2(1, 1)));
	vec2 u3 = (uv + (pixel_step * vec2(1, -1)));
	vec2 u4 = (uv + (pixel_step * vec2(-1, 1)));

	// TODO: displacement based on noise texture
	vec3 normal_sharpen = (texture(normal_tex, u0).rgb * 5.0f)
						- (texture(normal_tex, u1).rgb
						+ texture(normal_tex, u2).rgb
						+ texture(normal_tex, u3).rgb
						+ texture(normal_tex, u4).rgb);

	float depth_sharpen = (linear_depth(u0, inv_proj) * 5.0f)
						- linear_depth(u1, inv_proj)
						- linear_depth(u2, inv_proj)
						- linear_depth(u3,inv_proj)
						- linear_depth(u4, inv_proj);

	float normal_value = length(normal_sharpen) / sqrt(3.0f);
	float depth_value = clamp(abs(depth_sharpen) - linear_depth(uv, inv_proj), 0, 1);
	float final_value = smoothstep(0.71f, 1.0f, pow(normal_value + depth_value, 2.0f));

	return final_value;
}

float overlay(float scene_value, vec2 pixel_uv)
{
	vec4 overlay_pix = texture(overlay_tex, pixel_uv);
	float overlay_value = (clamp(overlay_pix.r + overlay_pix.g + overlay_pix.b, 0.0f, 1.0f) * overlay_pix.a * 2.0f);
	return mix(scene_value, float(scene_value < 0.5f), float(overlay_value > 0.5f));
}

vec3 colourise(float value)
{
	float final_value = clamp(value, 0.0f, 1.0f);
	vec3 final_colour = vec3(0.0f);
	if (use_palette_mid)
	{
		if (final_value > 0.5f)
			final_colour = mix(palette_mid, palette_high, smoothstep(0.0f, 1.0f, (final_value * 2.0f) - 1.0f));
		else
			final_colour = mix(palette_low, palette_mid, smoothstep(0.0f, 1.0f, (final_value * 2.0f)));
	}
	else
		final_colour = mix(palette_low, palette_high, smoothstep(0.0f, 1.0f, final_value));
	return final_colour;
}

vec3 srgb_to_linear(vec3 v)
{
	return mix(v / 12.92, pow((v + 0.055f) / 1.055f, vec3(2.4f)), vec3(greaterThan(v, vec3(0.04045f))));
}

vec3 linear_to_srgb(vec3 v)
{
	return mix(v * 12.92, (pow(v, vec3(1.0f / 2.4f)) * 1.055f) - 0.055f, vec3(greaterThan(v, vec3(0.0031308f))));
}

vec3 sample_lut(float f)
{
	float f_2 = clamp(f, 0.0f, 1.0f);
	float x = mod(f_2 * 64.0f, 1.0f);
	float y = 1.0f - (floor(f_2 * 64.0f) / 64.0f);
	return texture(lut, vec2(x, y)).rgb;
}

void fragment()
{
	vec2 downscaled_viewport_size = VIEWPORT_SIZE / float(pixelation_size);
	vec2 downscaled_uv = floor(SCREEN_UV * downscaled_viewport_size/* * float(pixelation_size)*/) / downscaled_viewport_size;
	//if (downscaled_uv.x > 1.0f || downscaled_uv.y > 1.0f)
	//	discard;
	
	vec3 screen_sample = texture(screen_tex, downscaled_uv).rgb;
	float screen_value = dot(screen_sample, vec3(0.2126f, 0.7151f, 0.0722f));
	if (texture(depth_tex, downscaled_uv).r == 0.0f)
		screen_value = 1.0f;
		
	vec2 edge_displace = vec2(
		fbm((downscaled_uv + 0.00f) / 0.07f, 3, 2.0f, 0.5f),
		fbm((downscaled_uv + 15.0f) / 0.07f, 3, 2.0f, 0.5f)
		);
	// compute edge mask via sharpen filters
	float edges = edge_mask((downscaled_uv + (edge_displace * 1.0f / downscaled_viewport_size)), 1.0f / VIEWPORT_SIZE, INV_PROJECTION_MATRIX);
	float edged_value = mix(screen_value, screen_value * 0.151f, edges);
	// apply grain
	float noise = round((fbm(downscaled_uv / 0.0005f / float(pixelation_size), 2, 2.0f, 0.03f) - 0.3f) * 8.0f) / 8.0f;
	float noised_value = edged_value + (noise * 0.01f);
	// posterise
	float posterised_value = round(noised_value * 64.0f) / 64.0f;
	// add sketch effect
	float sin_a = pow(sin(dot(downscaled_uv * 20.0f / float(pixelation_size), vec2(0.5f, -0.5f) * vec2(250, -500) / float(pixelation_size))), 2.0f);
	float sin_b = pow(sin(dot(downscaled_uv * 20.0f / float(pixelation_size), vec2(0.5f, -0.5f) * vec2(250, 450) / float(pixelation_size))), 2.0f);
	float sin_c = (float(sin_a > 0.3f) - 0.5f) + (float(sin_b > 0.3f) - 0.5f);
	float sketched_value = posterised_value + (clamp(0, 1, sqrt(posterised_value)) * sin_c * 0.061);
	
	ALBEDO = vec3(sketched_value);
	
	// compute the whole scene at standard scale, but DONT compute edge values for both pixels and then downscale (average) them
	// essentially only compute the top left corner of each block of 2x2, but use the whole buffer still
	// output into only the top quarter of the buffer (discard the rest based on uv)
	// then scale that output to fill the screen in the viewport (only 2 copies, UI into this, then this onto screen)
	
	float downscaled_scene_value = sketched_value;
	
	float overlaid_value = overlay(downscaled_scene_value, downscaled_uv);
	
	// colourise
	ALBEDO = (colourise(overlaid_value));
	//ALBEDO = sample_lut(sketched_value);
}
