shader_type spatial;
render_mode unshaded, blend_mul, shadows_disabled;

#define FRAGMENT_CODE ALBEDO = vec3(0.62f);
#define SNAPPING

#include "StandardShader.gdshaderinc"