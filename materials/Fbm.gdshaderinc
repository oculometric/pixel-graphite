const vec2 fbm_e = vec2(12.9898f,78.2330f);

float fbm_random(vec2 coord)
{
	return fract(sin(dot(coord, fbm_e)) * 43758.5f);
}

float hash(vec2 p)
{
    vec3 p3 = vec3(p, 1.0f);
    p3  = fract(p3*0.1031);
    p3 += dot(p3, p3.zyx+31.32);
    return fract((p3.x+p3.y)*p3.z);
}

float fbm_noise(vec2 coord)
{
    vec2 flr = floor(coord);
    vec2 frc = fract(coord);

    float tln = hash(flr + vec2(0,0));
    float trn = hash(flr + vec2(1,0));
    float bln = hash(flr + vec2(0,1));
    float brn = hash(flr + vec2(1,1));

    vec2 m = smoothstep(0.0f, 1.0f, frc);

    float result = mix(tln, trn, m.x) +
					(bln - tln) * m.y * (1.0f - m.x) +
					(brn - trn) * m.x * m.y;

    return (result * 2.0f) - 1.0f;
}

float fbm(vec2 _coord, int _octaves, float _lacunarity, float _gain)
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