#version 440

struct sSurfacePoint {
    int is_surface;
    vec3 position;
};

layout(std430, binding = 1) buffer vertices_surfaces {
    vec4 vertices[];
};

layout(std430, binding = 2) buffer raw_mesh {
    vec4 mesh_vertices[];
};

layout(std430, binding = 4) buffer mesh_indices {
    uvec4 indices[];
};

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

void main() {
    uint index = uint(gl_GlobalInvocationID.x);
    uint mesh_index = (index * 3u);
    uvec4 vertex_index = indices[index];
    mesh_vertices[mesh_index] = vertices[vertex_index.x];
    mesh_vertices[mesh_index+1] = vertices[vertex_index.y];
    mesh_vertices[mesh_index+2] = vertices[vertex_index.z];
}