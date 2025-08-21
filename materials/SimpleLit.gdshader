shader_type spatial;

#define TRIPLANAR
#define SNAPPING
#include "StandardShader.gdshaderinc"

uniform bool use_triplanar = false;

uniform sampler2D albedo : source_color, filter_nearest, hint_default_white;
uniform sampler2D normal : hint_normal, filter_nearest;

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

	ALBEDO = texture(albedo, uv).rgb;
	NORMAL_MAP = texture(normal, uv).rgb;
}