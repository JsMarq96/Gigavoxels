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

const uvec3 child_indexing = uvec3(1u, 2u, 4u);

void main() {
    // Node location: index in workgroup * 8 + index by local size ()
    const uint current_layer_position = uint(dot(gl_WorkGroupID, vec3(1.0, gl_NumWorkGroups.y, gl_NumWorkGroups.z * gl_NumWorkGroups.z))) * 8u + uint(dot(child_indexing, gl_LocalInvocationID));

    // Get the prev layer's starting position from the gl_GloblaInvocation
    // Thanks to the octree structure: prev_layer_starting_index = current_layer_id * 2
    const uvec3 prev_layer_position = gl_GlobalInvocationID * 2u;

    const uvec3 prev_layer_workgroup_size = uvec3(u_prev_layer_size / 2.0);
    const uint prev_layer_index_position = uint(dot(prev_layer_position, vec3(1.0, prev_layer_workgroup_size.y, prev_layer_workgroup_size.z * prev_layer_workgroup_size.z))) * 8u;

    // Choose the type of current block, based on the type of the children
    uint count = 0u;
    for(uint octant = 0u; octant < 8u; octant++) {
        count += octree[u_prev_layer_size + prev_layer_index_position + octant].is_leaf;
    }

    // Based on the total count, choose the kind of block that we are building
    if (count == 8u) {
        count = FULL_LEAF;
    } else if (count ==16u) {
        count = EMPTY_LEAF;
    } else {
        count = NON_LEAF;
    }

    octree[u_curr_layer_start + current_layer_position].is_leaf = count;
}