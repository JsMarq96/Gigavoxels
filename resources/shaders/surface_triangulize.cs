#version 440

struct sSurfacePoint {
    int is_surface;
    vec3 position;
};

layout(std430, binding = 1) buffer vertices_surfaces {
    vec4 vertices[];
};

layout(std430, binding = 2) buffer raw_mesh {
    ivec4 mesh_vertices[];
};

layout(binding = 3) uniform atomic_uint mesh_vertices_count;

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
        uint start_vert = atomicCounterIncrement(mesh_vertices_count);
        mesh_vertices[start_vert] = ivec4(t1_v1, t1_v2, index, 0);
    }

    // Test second triangle
    if (vertices[t2_v1].w != 0.0 && vertices[t2_v2].w != 0.0) { // Early out
        uint start_vert = atomicCounterIncrement(mesh_vertices_count);
        mesh_vertices[start_vert] = ivec4(t2_v1, t2_v2, index, 0);
    }
}

void test_triangle_y(int index, in ivec3 curr_pos, in vec4 current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[1]);
    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[3]);

    // Test first triangle
    if (vertices[t1_v1].w != 0.0 && vertices[t1_v2].w != 0.0) { // Early out
        uint start_vert = atomicCounterIncrement(mesh_vertices_count);
        mesh_vertices[start_vert] = ivec4(t1_v1, t1_v2, index, 0);
    }

    // Test second triangle
    if (vertices[t2_v1].w != 0.0 && vertices[t2_v2].w != 0.0) { // Early out
        uint start_vert = atomicCounterIncrement(mesh_vertices_count);
        mesh_vertices[start_vert] = ivec4(t2_v1, t2_v2, index, 0);
    }
}

void test_triangle_z(int index, in ivec3 curr_pos, in vec4 current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[1]);
    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[3]);

    // Test first triangle
    if (vertices[t1_v1].w != 0.0 && vertices[t1_v2].w != 0.0) { // Early out
        uint start_vert = atomicCounterIncrement(mesh_vertices_count);
        mesh_vertices[start_vert] = ivec4(t1_v1, t1_v2, index, 0);
    }

    // Test second triangle
    if (vertices[t2_v1].w != 0.0 && vertices[t2_v2].w != 0.0) { // Early out
        uint start_vert = atomicCounterIncrement(mesh_vertices_count);
        mesh_vertices[start_vert] = ivec4(t2_v1, t2_v2, index, 0);
    }
}

/*
void test_triangle_y(in ivec3 curr_pos, in sSurfacePoint current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[1]);
    // Test first triangle
    if (vertices[t1_v1].is_surface != 0 && vertices[t1_v1].is_surface != 255 && vertices[t1_v2].is_surface != 0 && vertices[t1_v2].is_surface != 255) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t1_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t1_v2].position, 0.0);
    }

    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[3]);
    // Test first triangle
    if (vertices[t2_v1].is_surface != 0 && vertices[t2_v1].is_surface != 255 && vertices[t2_v2].is_surface != 0 && vertices[t2_v2].is_surface != 255) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t2_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t2_v2].position, 0.0);
    }
}

void test_triangle_z(in ivec3 curr_pos, in sSurfacePoint current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[1]);
    // Test first triangle
    if (vertices[t1_v1].is_surface != 0 && vertices[t1_v1].is_surface != 255 && vertices[t1_v2].is_surface != 0 && vertices[t1_v2].is_surface != 255) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t1_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t1_v2].position, 0.0);
    }

    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[3]);
    // Test first triangle
    if (vertices[t2_v1].is_surface != 0 && vertices[t2_v1].is_surface != 255 && vertices[t2_v2].is_surface != 0 && vertices[t2_v2].is_surface != 255) { // Early out
        uint start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t2_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t2_v2].position, 0.0);
    }
}*/

void main() {
    ivec3 post = ivec3(gl_GlobalInvocationID);
    int index = get_index_of_position(post);

    vec4 current_point = vertices[index];

    //atomicAdd(mesh_vertices_count, 1);

    if (current_point.w == 0.0) { // Early out
        return;
    }
    
    //uint start_vert = atomicAdd(mesh_vertices_count, 3);
    //mesh_vertices[start_vert] = vec4(float(current_point.is_surface), 0.0, 0.0, 0.0);
    //mesh_vertices[start_vert+1] = vec4(float(current_point.is_surface), 0.0, 0.0, 0.0);
    //mesh_vertices[start_vert+2] = vec4(float(current_point.is_surface), 0.0, 0.0, 0.0);
    test_triangle_x(index, post, current_point);
    test_triangle_y(index, post, current_point);
    test_triangle_z(index, post, current_point);
    /*int axis_seed = int(current_point.w);
    if (((axis_seed & 4) == 4) || ((axis_seed & 8) == 8)) {
        test_triangle_x(index, post, current_point);
    }
    
    if (((axis_seed & 1) == 1) || ((axis_seed & 2) == 2)) {
        test_triangle_y(index, post, current_point);
    }

    if (((axis_seed & 16) == 16) || ((axis_seed & 32) == 32)) {
        test_triangle_z(index, post, current_point);
    }*/
    //test_triangle_y(post, current_point);
    //test_triangle_z(post, current_point);
}