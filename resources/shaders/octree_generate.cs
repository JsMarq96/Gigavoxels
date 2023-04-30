#version 440

layout (local_size_x = 2, local_size_y = 2, local_size_z = 2) in;

struct sOctreeNode {
    uint is_leaf;
    uint child_index;
    vec2 _padding;
};

layout(std430, binding=2) buffer octree_ssbo {
    sOctreeNode octree[];
};

uniform uint      u_curr_layer_start;
uniform uint      u_prev_layer_start;
uniform vec3      u_curr_layer_size;
uniform vec3      u_prev_layer_size;

const uint NON_LEAF = 0u;
const uint FULL_LEAF = 1u;
const uint EMPTY_LEAF = 2u;

void main() {
    vec3 location = gl_GlobalInvocationID / u_curr_layer_size;
    vec3 prev_location = location * u_prev_layer_size;

    uint curr_local_pos = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * u_curr_layer_size.y + gl_GlobalInvocationID.z * u_curr_layer_size.z * u_curr_layer_size.z;
    uint prev_local_pos = curr_local_pos.x + curr_local_pos.y * u_prev_layer_size.y + curr_local_pos.z * u_prev_layer_size.z * u_prev_layer_size.z;


}