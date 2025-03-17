shader_type spatial;

#include "Fbm.gdshaderinc"

render_mode unshaded, cull_back, fog_disabled;

uniform sampler2D screen_tex : source_color, filter_nearest, hint_screen_texture;
uniform sampler2D normal_tex : filter_nearest, hint_normal_roughness_texture;
uniform sampler2D depth_tex : filter_nearest, hint_depth_texture;
uniform vec3 low_colour : source_color = vec3(0.023f, 0.021f, 0.060f);
uniform vec3 high_colour : source_color = vec3(2.400f, 1.831f, 1.728f);

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
	
	// TODO: displace edge UV
	// compute edge mask via sharpen filters
	float edges = edge_mask(SCREEN_UV, 1.0f / VIEWPORT_SIZE, INV_PROJECTION_MATRIX);
	float edged_value = mix(screen_value, screen_value * 0.151f, edges);
	
	// TODO: add pixel noise
	// posterise
	float posterised_value = round(edged_value * 64.0f) / 64.0f;
	// TODO: add sketch effect
	// TODO: displace
	
	// TODO: move downscaling and colouring code into separate shader combining rendered scene and UI
	vec3 final_colour = mix(linear_to_srgb(low_colour), linear_to_srgb(high_colour), clamp(smoothstep(0.0f, 1.0f, posterised_value), 0.0f, 1.0f));
	
	ALBEDO = vec3(final_colour);
	ALBEDO = vec3(fbm(vec3(SCREEN_UV, 0) / 0.05f, 2, 2.0f, 0.03f));
}