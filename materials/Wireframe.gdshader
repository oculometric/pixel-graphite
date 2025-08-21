shader_type spatial;
render_mode wireframe, unshaded, cull_disabled;

#define SNAPPING
#include "StandardShader.gdshaderinc"

void vertex()
{
	SNAP;
}

void fragment()
{
	ALBEDO = vec3(0.0f);
}