#version 440

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

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

const uint NON_LEAF = 0u;
const uint FULL_LEAF = 1u;
const uint EMPTY_LEAF = 2u;

void main() {
    // trick form https://stackoverflow.com/questions/49335851/compute-shader-gl-globalinvocationid-and-local-size
    const uint cluster_size = gl_WorkGroupSize.x * gl_WorkGroupSize.y * gl_WorkGroupSize.z;
    const uvec3 linearized_invocation = uvec3(1u, clusterSize, clusterSize * clusterSize);
    uint global_id = dot(clusterSize, gl_GlobalInvocationID);

    const float depth = imageLoad(u_volume_texture, gl_GlobalInvocationID.xyz).r;

    octree[u_layer_start + global_id].is_leaf = uint(step(0.15, depth)) + 1u; // if full (1) or empty (2)
}