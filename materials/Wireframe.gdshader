shader_type spatial;
render_mode wireframe, unshaded, cull_disabled;

uniform sampler2D screen_tex : source_color, filter_nearest, hint_screen_texture;

void vertex()
{
}

void fragment()
{
	ALBEDO = vec3(0.0f);//vec3(texture(screen_tex, SCREEN_UV).r < 0.5f ? 1.0f : 0.0f);
}