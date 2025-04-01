shader_type spatial;

#include "Voronoi.gdshaderinc"

varying vec3 world_position;

float fresnel(float amount, vec3 normal, vec3 view)
{
	return pow((1.0 - clamp(dot(normalize(normal), normalize(view)), 0.0, 1.0 )), amount);
}

void vertex()
{
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1)).xyz;
	// TODO: voronoi sand displacement
}

void fragment()
{
	float value = 1.2f;
	float fres = fresnel(3.0f, NORMAL, VIEW) * 2.0f;

	float threshold = mix(0.18f, 0.95f, fres);
	value = clamp(clamp(float(voronoi(world_position.xz * 20.0f, 4.0f, 0.96f, 0.5f) > threshold) + 0.3f, 0, 1)
		  *             float(voronoi(world_position.xz * 12.0f, 4.0f, 0.96f, 0.5f) > threshold), 0, 1);
	value = mix(0.189f, 1.08f, value) + 0.15f;

	ALBEDO = vec3(value);
}