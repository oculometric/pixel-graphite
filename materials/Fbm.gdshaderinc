const vec3 fbm_e = vec3(12.9898f,78.2330f,35.6341f);

float fbm_random(vec3 coord)
{
	return fract(sin(dot(coord, fbm_e)) * 43758.5f);
}

float fbm_noise(vec3 coord)
{
    vec3 flr = floor(coord);
    vec3 frc = fract(coord);

    float tln = fbm_random(flr + vec3(0,0,0));
    float trn = fbm_random(flr + vec3(1,0,0));
    float bln = fbm_random(flr + vec3(0,1,0));
    float brn = fbm_random(flr + vec3(1,1,0));
    float tlf = fbm_random(flr + vec3(0,0,1));
    float trf = fbm_random(flr + vec3(1,0,1));
    float blf = fbm_random(flr + vec3(0,1,1));
    float brf = fbm_random(flr + vec3(1,1,1));

    vec3 m = smoothstep(0.0f, 1.0f, frc);

    float result =
    mix(
        mix(
            mix(tln, trn, m.x),
            mix(bln, brn, m.x),
            m.y
        ),
        mix(
            mix(tlf, trf, m.x),
            mix(blf, brf, m.x),
            m.y
        ),
        m.z
    );

    return (result * 2.0f) - 1.0f;
}

float fbm(vec3 _coord, int _octaves, float _lacunarity, float _gain)
{
    float amplitude = 1.0;
    float frequency = 1.0;

    float max_amplitude = 0.0;

    float v = 0.0;

    for (int i = 0; i < _octaves; i++)
    {
        v += fbm_noise(_coord * frequency) * amplitude;
        frequency *= _lacunarity;
        max_amplitude += amplitude;
        amplitude *= _gain;
    }

    v /= max_amplitude;

    return v;
}