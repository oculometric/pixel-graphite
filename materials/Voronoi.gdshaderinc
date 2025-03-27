float hash(vec3 v)
{
    return fract(sin(dot(v, vec3(201.0f, 123.0f, 304.2f))) * 190493.02095f) * 2.0f - 1.0f;
}

// returns the distance metric for a given position in 3D voronoi nosie
float voronoi(vec3 position, float randomness, float exponent)
{
    vec3 cell = floor(position);
    float closest = 8.0f;

    for (int k = -1; k <= 1; k++)
    {
        for (int j = -1; j <= 1; j++)
        {
            for (int i = -1; i <= 1; i++)
            {
				vec3 test_point = cell + vec3(ivec3(i, j, k));
				vec3 noise = (vec3(hash(test_point.xyz), hash(test_point.yzx), hash(test_point.zxy)) * randomness * 0.5f);
                test_point += noise;

				vec3 diff = position - test_point;
                float dist = pow(dot(pow(abs(diff), vec3(exponent)), vec3(1.0f)), 1.0f / exponent);
                if (dist < closest)
                    closest = dist;
            }
        }
    }

	return closest;
}

float voronoi(vec2 position, float scale, float randomness, float exponent)
{
    vec2 cell = floor(position);
    float closest = 8.0f;

    for (int j = -1; j <= 1; j++)
    {
        for (int i = -1; i <= 1; i++)
        {
			vec2 test_point = cell + (vec2(ivec2(i, j)));
			vec2 noise = (vec2(hash(vec3(test_point.xy, 0.0f)), hash(vec3(test_point.yx, 0.0f))) * randomness * 0.5f);
            test_point += noise;

			vec2 diff = (position) - test_point;
            float dist = pow(dot(pow(abs(diff), vec2(exponent)), vec2(1.0f)), 1.0f / exponent);
            if (dist < closest) closest = dist;
        }
    }

	return closest;
}
