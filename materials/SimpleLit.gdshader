shader_type spatial;

uniform bool use_triplanar = false;
uniform float triplanar_size = 1.0f;
uniform float triplanar_offset = 0.0f;

varying vec3 world_position;
varying vec3 world_normal;

uniform sampler2D albedo : source_color, filter_nearest, hint_default_white;
uniform sampler2D normal : hint_normal, filter_nearest;

void vertex()
{
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1)).xyz;
	world_normal = normalize((MODEL_NORMAL_MATRIX * NORMAL).xyz);
}

void fragment()
{
	vec2 uv;
	if (use_triplanar) uv = ((abs(world_normal.y) < 0.707 ? (abs(world_normal.z) < 0.707 ? world_position.zy : world_position.xy) : world_position.xz) * vec2(1.0, -1.0) / triplanar_size) + vec2(triplanar_offset);
	else uv = UV;

	ALBEDO = texture(albedo, uv).rgb;
	NORMAL_MAP = texture(normal, uv).rgb;
}