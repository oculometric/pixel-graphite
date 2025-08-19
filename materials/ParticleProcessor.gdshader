shader_type particles;
render_mode disable_velocity, keep_data, disable_force;

#include "Fbm.gdshaderinc"

uniform float initial_angle_min;
uniform float initial_angle_max;
uniform vec3 emission_box_extents;
uniform vec3 emission_box_offset;
uniform float scale_min;
uniform float scale_max;

float rand_from_seed(inout uint seed)
{
	int k;
	int s = int(seed);
	if (s == 0) s = 305420679;
	k = s / 127773;
	s = 16807 * (s - k * 127773) - 2836 * k;
	if (s < 0) s += 2147483647;
	seed = uint(s);
	return float(seed % uint(65536)) / 65535.0;
}

uint hash(uint x) {
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = (x >> uint(16)) ^ x;
	return x;
}

vec3 calculate_initial_position(inout uint alt_seed) {
	float pi = 3.14159;
	vec3 pos = vec3(rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0) * emission_box_extents;
	return pos + emission_box_extents;
}

void start() {
	uint base_number = NUMBER;
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);

	float scale = mix(scale_min, scale_max, rand_from_seed(alt_seed));
	vec3 position = ((vec3(rand_from_seed(alt_seed), rand_from_seed(alt_seed), rand_from_seed(alt_seed)) * 2.0f - 1.0f) * emission_box_extents) + emission_box_offset;

	if (fbm(position.xz, 3, 2.0f, 0.5f) < 0.0f)
		ACTIVE = false;

	TRANSFORM[0].xyz = vec3(scale, 0.0, 0.0);
	TRANSFORM[1].xyz = vec3(0.0, scale, 0.0);
	TRANSFORM[2].xyz = vec3(0.0, 0.0, scale * 1.5f);
	TRANSFORM[3].xyz = position;
	TRANSFORM = EMISSION_TRANSFORM * TRANSFORM;
}

void process()
{
	uint base_number = NUMBER;
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);

	CUSTOM.y += DELTA / LIFETIME;
	CUSTOM.y = mix(CUSTOM.y, 1.0, INTERPOLATE_TO_END);

	COLOR = vec4(1.0f);
}
