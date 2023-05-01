#version 440

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

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

const uvec3 child_indexing = uvec3(1u, 2u, 4u);

void main() {
    octree[0].is_leaf = NON_LEAF;
    octree[0].child_index = 1;
}