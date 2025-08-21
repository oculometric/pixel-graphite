#ifdef TRIPLANAR
uniform float triplanar_scale = 1.0f;
uniform float triplanar_offset = 0.0f;
varying vec3 world_position;
varying vec3 world_normal;
#define TRIPLANAR_MAP world_position = (MODEL_MATRIX * vec4(VERTEX, 1)).xyz; world_normal = normalize((MODEL_NORMAL_MATRIX * NORMAL).xyz);
#define TRIPLANAR_UV ((abs(world_normal.y) < 0.707 ? (abs(world_normal.z) < 0.707 ? world_position.zy : world_position.xy) : world_position.xz) * vec2(1.0, -1.0) / triplanar_scale)
#endif

#ifdef SNAPPING
const float snapping_scale = 0.5f;
varying vec4 unsnapped_position;

vec4 snapXY(vec4 projected_position, vec2 resolution)
{
	vec3 snapped_position = projected_position.xyz / projected_position.w;
	snapped_position.xy = round(snapped_position.xy * resolution * snapping_scale) / (resolution * snapping_scale);
	snapped_position *= projected_position.w;

	return vec4(snapped_position, projected_position.w);
}
#define SNAP unsnapped_position = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1); POSITION = snapXY(unsnapped_position, VIEWPORT_SIZE);
#endif
