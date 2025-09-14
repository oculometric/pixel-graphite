shader_type spatial;

#define TRIPLANAR
#define SNAPPING
#include "StandardShader.gdshaderinc"

uniform bool use_triplanar = false;

uniform sampler2D albedo : source_color, filter_nearest, hint_default_white;
uniform sampler2D normal : hint_normal, filter_nearest;

uniform bool use_separate_top = false;
uniform sampler2D albedo_top : source_color, filter_nearest, hint_default_white;
uniform sampler2D normal_top : hint_normal, filter_nearest;

void vertex()
{
	TRIPLANAR_MAP;
	SNAP;
}

void fragment()
{
	vec2 uv;
	if (use_triplanar) uv = TRIPLANAR_UV;
	else uv = UV;

	bool is_top_face = dot(world_normal, vec3(0, 1, 0)) > 0.717f;

	if (is_top_face && use_separate_top)
	{
		ALBEDO = texture(albedo_top, uv).rgb;
		NORMAL_MAP = texture(normal_top, uv).rgb;
	}
	else
	{
		ALBEDO = texture(albedo, uv).rgb;
		NORMAL_MAP = texture(normal, uv).rgb;
	}
}