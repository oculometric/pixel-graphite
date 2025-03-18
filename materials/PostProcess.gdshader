shader_type spatial;

#include "Fbm.gdshaderinc"

render_mode unshaded, cull_back, fog_disabled;

uniform sampler2D screen_tex : source_color, filter_nearest, hint_screen_texture;
uniform sampler2D normal_tex : filter_nearest, hint_normal_roughness_texture;
uniform sampler2D depth_tex : filter_nearest, hint_depth_texture;

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
	// TODO: displacement based on noise texture
	vec3 normal_sharpen = (texture(normal_tex, uv).rgb * 5.0f)
						- (texture(normal_tex, uv + (pixel_step * vec2(-1, -1))).rgb
						+ texture(normal_tex, uv + (pixel_step * vec2(1, 1))).rgb
						+ texture(normal_tex, uv + (pixel_step * vec2(1, -1))).rgb
						+ texture(normal_tex, uv + (pixel_step * vec2(-1, 1))).rgb);

	float depth_sharpen = (linear_depth(uv, inv_proj) * 5.0f)
						- linear_depth(uv + (pixel_step * vec2(-1, -1)), inv_proj)
						- linear_depth(uv + (pixel_step * vec2(1, 1)), inv_proj)
						- linear_depth(uv + (pixel_step * vec2(1, -1)),inv_proj)
						- linear_depth(uv + (pixel_step * vec2(-1, 1)), inv_proj);

	float normal_value = length(normal_sharpen) / sqrt(3.0f);
	float depth_value = clamp(abs(depth_sharpen) - linear_depth(uv, inv_proj), 0, 1);
	float final_value = smoothstep(0.71f, 1.0f, pow(normal_value + depth_value, 2.0f));

	return final_value;
}

void fragment()
{
	vec3 screen_sample = texture(screen_tex, SCREEN_UV).rgb;
	float screen_value = dot(screen_sample, vec3(0.2126f, 0.7151f, 0.0722f));
	if (texture(depth_tex, SCREEN_UV).r == 0.0f)
		screen_value = 1.0f;

	// TODO: move ambient occlusion into a separate shader? if we can render the same scene twice into different viewports then we can do custom AO in one

	// displace edge UV
	vec2 edge_displace = vec2(
		fbm((SCREEN_UV + 0.00f) / 0.01f, 3, 2.0f, 0.5f),
		fbm((SCREEN_UV + 15.0f) / 0.01f, 3, 2.0f, 0.5f)
		);
	// compute edge mask via sharpen filters
	float edges = edge_mask(SCREEN_UV + (edge_displace * 1.2f / VIEWPORT_SIZE), 1.0f / VIEWPORT_SIZE, INV_PROJECTION_MATRIX);
	float edged_value = mix(screen_value, screen_value * 0.151f, edges);

	// add pixel noise
	float noise = round((fbm(SCREEN_UV / 0.0005f, 2, 2.0f, 0.03f) - 0.5f) * 8.0f) / 8.0f;
	float noised_value = edged_value + (noise * 0.018f);
	// posterise
	float posterised_value = round(noised_value * 64.0f) / 64.0f;
	// add sketch effect
	float sin_a = pow(sin(dot(SCREEN_UV * 20.0f, vec2(0.5f, -0.5f) * vec2(250, -500))), 2.0f);
	float sin_b = pow(sin(dot(SCREEN_UV * 20.0f, vec2(0.5f, -0.5f) * vec2(250, 450))), 2.0f);
	float sin_c = (float(sin_a > 0.3f) - 0.5f) + (float(sin_b > 0.3f) - 0.5f);
	float sketched_value = posterised_value + (clamp(0, 1, sqrt(posterised_value)) * sin_c * 0.061);

	ALBEDO = vec3(sketched_value);
}