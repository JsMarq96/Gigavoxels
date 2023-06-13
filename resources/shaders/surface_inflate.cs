#version 440

layout(std430, binding = 1) buffer vertices_surfaces {
    vec4 vertices[];
};

layout(std430, binding = 4) buffer mesh_indices {
    uvec4 indices[];
};

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const float INFLATION_STEP = 0.2;

void main() {
    uint index = uint(gl_GlobalInvocationID.x);
    uint mesh_index = (index * 3u);
    uvec4 vertex_index = indices[index];
    
    // Compute face normal
    vec3 v = vertices[vertex_index.y].xyz - vertices[vertex_index.x].xyz;
    vec3 w = vertices[vertex_index.z].xyz - vertices[vertex_index.x].xyz;

    vec3 n = vec3((v.y * w.z) - (v.z * w.y), (v.z * w.x) - (v.x * w.z), (v.x * w.y) - (v.y * w.x));
    n = normalize(-n);

    // Inflate mesh on the face normal direction
    vertices[vertex_index.x] = vec4(vertices[vertex_index.x].xyz + n  * INFLATION_STEP * 0.15, 0.0);
    vertices[vertex_index.y] = vec4(vertices[vertex_index.y].xyz + n * INFLATION_STEP * 0.15, 0.0);
    vertices[vertex_index.z] = vec4(vertices[vertex_index.z].xyz + n * INFLATION_STEP * 0.15, 0.0);

    //vertices[vertex_index.x] = vec4(n, 0.0);
    //vertices[vertex_index.y] = vec4(n, 0.0);
    //vertices[vertex_index.z] = vec4(n, 0.0);
}