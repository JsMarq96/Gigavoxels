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

uniform sampler3D u_volume_map;
uniform uint      u_layer_start;
uniform uint      u_img_dimm;

const uint NON_LEAF = 0u;
const uint FULL_LEAF = 1u;
const uint EMPTY_LEAF = 2u;

const uvec3 child_indexing = uvec3(1u, 2u, 4u);

void main() {
    const uvec3 coords = uvec3(gl_WorkGroupID.x / (u_img_dimm * u_img_dimm), (gl_WorkGroupID.x / u_img_dimm) % u_img_dimm, gl_WorkGroupID.x % u_img_dimm);
    // Node location: index in workgroup * 8 + index by local size ()
    const uint in_octree_memory = uint(gl_WorkGroupID.x) * 8u + uint(dot(child_indexing, gl_LocalInvocationID));

    const float depth = texelFetch(u_volume_map, ivec3(uvec3(coords) * 2 + gl_LocalInvocationID),0).r;

    octree[u_layer_start + in_octree_memory].is_leaf = uint(step(depth, 0.15)) + 1u; // if full (1) or empty (2)
    octree[u_layer_start + in_octree_memory].child_index = coords.x;// / (u_img_dimm * u_img_dimm);
    octree[u_layer_start + in_octree_memory]._padding = vec2(coords.yz);
}