shader_type spatial;

#include "Voronoi.gdshaderinc"

varying vec3 world_position;

void vertex()
{
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1)).xyz;
	// TODO: voronoi sand displacement
}

void fragment()
{
	float value = 1.2f;

	float threshold = 0.2f;
	value = clamp(clamp(float(voronoi(world_position.xz * 24.0f, 4.0f, 0.96f, 0.5f) > 0.2f) + 0.3f, 0, 1)
		  *             float(voronoi(world_position.xz * 16.0f, 4.0f, 0.96f, 0.5f) > 0.2f), 0, 1);
	value = mix(0.189f, 1.0f, value);

	// TODO: fresnel
	vec4 view_position = INV_PROJECTION_MATRIX * vec4(VERTEX, 1);
	vec3 view_direction = normalize(view_position.xyz / view_position.w);

	value = dot(view_direction, NORMAL);
	ALBEDO = vec3(1.0f - value);
}