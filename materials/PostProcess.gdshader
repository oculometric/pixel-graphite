shader_type spatial;

render_mode unshaded, cull_back, fog_disabled;

uniform sampler2D screen_tex : source_color, filter_nearest, hint_screen_texture;
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

void fragment()
{
	vec3 screen_sample = texture(screen_tex, SCREEN_UV).rgb;
	float screen_value = dot(screen_sample, vec3(0.2126f, 0.7151f, 0.0722f));
	if (texture(depth_tex, SCREEN_UV).r == 0.0f)
		screen_value = 1.0f;
	vec3 final_colour = mix(linear_to_srgb(low_colour), linear_to_srgb(high_colour), clamp(smoothstep(0.0f, 1.0f, screen_value), 0.0f, 1.0f));
	
	ALBEDO = final_colour;
}