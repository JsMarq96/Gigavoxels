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

layout(std430, binding = 3) buffer vertex_count {
    uint mesh_vertices_count;
};

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const ivec3 TEST_AXIS_Z[4] = ivec3[4](
    ivec3(1, 0, 0),
    ivec3(0, 1, 0),
    ivec3(-1, 0, 0),
    ivec3(0, -1, 0)
);

const ivec3 TEST_AXIS_X[4] = ivec3[4](
    ivec3(0, 1, 0),
    ivec3(0, 0, 1),
    ivec3(0, -1, 0),
    ivec3(0, 0, -1)
);

const ivec3 TEST_AXIS_Y[4] = ivec3[4](
    ivec3(1, 0, 0),
    ivec3(0, 0, 1),
    ivec3(-1, 0, 0),
    ivec3(0, 0, -1)
);

const vec3 box_indices[36] = vec3[36](
vec3( -1.0 ,  1.0 ,  -1.0 ),
vec3( 1.0 ,  1.0 ,  1.0 ),
vec3( 1.0 ,  1.0 ,  -1.0 ),
vec3( 1.0 ,  1.0 ,  1.0 ),
vec3( -1.0 ,  -1.0 ,  1.0 ),
vec3( 1.0 ,  -1.0 ,  1.0 ),
vec3( -1.0 ,  1.0 ,  1.0 ),
vec3( -1.0 ,  -1.0 ,  -1.0 ),
vec3( -1.0 ,  -1.0 ,  1.0 ),
vec3( 1.0 ,  -1.0 ,  -1.0 ),
vec3( -1.0 ,  -1.0 ,  1.0 ),
vec3( -1.0 ,  -1.0 ,  -1.0 ),
vec3( 1.0 ,  1.0 ,  -1.0 ),
vec3( 1.0 ,  -1.0 ,  1.0 ),
vec3( 1.0 ,  -1.0 ,  -1.0 ),
vec3( -1.0 ,  1.0 ,  -1.0 ),
vec3( 1.0 ,  -1.0 ,  -1.0 ),
vec3( -1.0 ,  -1.0 ,  -1.0 ),
vec3( -1.0 ,  1.0 ,  -1.0 ),
vec3( -1.0 ,  1.0 ,  1.0 ),
vec3( 1.0 ,  1.0 ,  1.0 ),
vec3( 1.0 ,  1.0 ,  1.0 ),
vec3( -1.0 ,  1.0 ,  1.0 ),
vec3( -1.0 ,  -1.0 ,  1.0 ),
vec3( -1.0 ,  1.0 ,  1.0 ),
vec3( -1.0 ,  1.0 ,  -1.0 ),
vec3( -1.0 ,  -1.0 ,  -1.0 ),
vec3( 1.0 ,  -1.0 ,  -1.0 ),
vec3( 1.0 ,  -1.0 ,  1.0 ),
vec3( -1.0 ,  -1.0 ,  1.0 ),
vec3( 1.0 ,  1.0 ,  -1.0 ),
vec3( 1.0 ,  1.0 ,  1.0 ),
vec3( 1.0 ,  -1.0 ,  1.0 ),
vec3( -1.0 ,  1.0 ,  -1.0 ),
vec3( 1.0 ,  1.0 ,  -1.0 ),
vec3( 1.0 ,  -1.0 ,  -1.0 )
);

ivec3 num_work_groups = ivec3(gl_NumWorkGroups.xyz);

int num_work_groups_z_p2 = (num_work_groups.z * num_work_groups.z);

int get_index_of_position(in ivec3 position) {
    return int(position.x + position.y * num_work_groups.y + position.z * num_work_groups_z_p2);
}

void test_triangle_x(int index, in ivec3 curr_pos, in vec4 current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_X[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_X[1]);
    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_X[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_X[3]);

    // Test first triangle
    if (vertices[t1_v1].w != 0.0 && vertices[t1_v2].w != 0.0) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 1);
        indices[start_vert] = uvec4(index, t1_v1, t1_v2, 0u);
    }

    // Test second triangle
    if (vertices[t2_v1].w != 0.0 && vertices[t2_v2].w != 0.0) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 1);
        indices[start_vert] = uvec4(index, t2_v1, t2_v2, 0u);
    }
}

void test_triangle_y(int index, in ivec3 curr_pos, in vec4 current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[1]);
    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[3]);

    // Test first triangle
    if (vertices[t1_v1].w != 0.0 && vertices[t1_v2].w != 0.0) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 1);
        indices[start_vert] = uvec4(index, t1_v1, t1_v2, 0u);
    }

    // Test second triangle
    if (vertices[t2_v1].w != 0.0 && vertices[t2_v2].w != 0.0) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 1);
        indices[start_vert] = uvec4(index, t2_v1, t2_v2, 0u);
    }
}

void test_triangle_z(int index, in ivec3 curr_pos, in vec4 current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[1]);
    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[3]);

    // Test first triangle
    if (vertices[t1_v1].w != 0.0 && vertices[t1_v2].w != 0.0) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 1);
        indices[start_vert] = uvec4(index, t1_v1, t1_v2, 0u);
    }

    // Test second triangle
    if (vertices[t2_v1].w != 0.0 && vertices[t2_v2].w != 0.0) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 1);
        indices[start_vert] = uvec4(index, t2_v1, t2_v2, 0u);
    }
}


void main() {
    ivec3 post = ivec3(gl_GlobalInvocationID);
    int index = get_index_of_position(post);

    vec4 current_point = vertices[index];

    //atomicAdd(mesh_vertices_count, 1);

    if (current_point.w == 0.0) { // Early out
        return;
    }

    float box_size = 2.0 / 25.0;
    uint start_vert = atomicAdd(mesh_vertices_count, 36);

    for(uint i = 0; i < 36; i++) {
        mesh_vertices[start_vert + i] = vec4(current_point.xyz + (box_indices[i] / 2.0) * box_size, 0.0);
    }
}