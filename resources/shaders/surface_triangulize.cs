#version 440

struct sSurfacePoint {
    vec3 position;
    int is_surface;
};

layout(std430, binding = 1) buffer vertices_surfaces {
    sSurfacePoint vertices[];
};

layout(std430, binding = 2) buffer raw_mesh {
    int mesh_vertices_count;
    vec3 mesh_vertices[];
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

uint num_work_groups_z_p2 = gl_NumWorkGroups.z * gl_NumWorkGroups.z;

uint get_index_of_position(in uvec3 position) {
    return position.x + position.y * gl_NumWorkGroups.y + position.z * num_work_groups_z_p2;
}

void test_triangle_x(in uvec3 curr_pos, in sSurfacePoint current_point) {
    uint t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_X[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_X[1]);
    // Test first triangle
    if (vertices[t1_v1].is_surface != 0 && vertices[t1_v1].is_surface != 255 && vertices[t1_v2].is_surface != 0 && vertices[t1_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = current_point.position;
        mesh_vertices[start_vert+1] = vertices[t1_v1].position;
        mesh_vertices[start_vert+2] = vertices[t1_v2].position;
    }

    uint t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_X[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_X[3]);
    // Test first triangle
    if (vertices[t2_v1].is_surface != 0 && vertices[t2_v1].is_surface != 255 && vertices[t2_v2].is_surface != 0 && vertices[t2_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = current_point.position;
        mesh_vertices[start_vert+1] = vertices[t2_v1].position;
        mesh_vertices[start_vert+2] = vertices[t2_v2].position;
    }
}

void test_triangle_y(in uvec3 curr_pos, in sSurfacePoint current_point) {
    uint t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[1]);
    // Test first triangle
    if (vertices[t1_v1].is_surface != 0 && vertices[t1_v1].is_surface != 255 && vertices[t1_v2].is_surface != 0 && vertices[t1_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = current_point.position;
        mesh_vertices[start_vert+1] = vertices[t1_v1].position;
        mesh_vertices[start_vert+2] = vertices[t1_v2].position;
    }

    uint t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[3]);
    // Test first triangle
    if (vertices[t2_v1].is_surface != 0 && vertices[t2_v1].is_surface != 255 && vertices[t2_v2].is_surface != 0 && vertices[t2_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = current_point.position;
        mesh_vertices[start_vert+1] = vertices[t2_v1].position;
        mesh_vertices[start_vert+2] = vertices[t2_v2].position;
    }
}

void test_triangle_z(in uvec3 curr_pos, in sSurfacePoint current_point) {
    uint t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[1]);
    // Test first triangle
    if (vertices[t1_v1].is_surface != 0 && vertices[t1_v1].is_surface != 255 && vertices[t1_v2].is_surface != 0 && vertices[t1_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = current_point.position;
        mesh_vertices[start_vert+1] = vertices[t1_v1].position;
        mesh_vertices[start_vert+2] = vertices[t1_v2].position;
    }

    uint t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[3]);
    // Test first triangle
    if (vertices[t2_v1].is_surface != 0 && vertices[t2_v1].is_surface != 255 && vertices[t2_v2].is_surface != 0 && vertices[t2_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = current_point.position;
        mesh_vertices[start_vert+1] = vertices[t2_v1].position;
        mesh_vertices[start_vert+2] = vertices[t2_v2].position;
    }
}

void main() {
    uint index = get_index_of_position(gl_GlobalInvocationID);

    sSurfacePoint current_point = vertices[index];

    //atomicAdd(mesh_vertices_count, 1);

    if (current_point.is_surface == 0 && current_point.is_surface == 255) { // Early out
        return;
    }
    
    test_triangle_x(gl_GlobalInvocationID, current_point);
    test_triangle_y(gl_GlobalInvocationID, current_point);
    test_triangle_z(gl_GlobalInvocationID, current_point);
}