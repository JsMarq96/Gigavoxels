#version 440

struct sSurfacePoint {
    int is_surface;
    vec3 position;
};

layout(std430, binding = 1) buffer vertices_surfaces {
    sSurfacePoint vertices[];
};

layout(std430, binding = 2) buffer raw_mesh {
    int mesh_vertices_count;
    vec3 _align;
    vec4 mesh_vertices[];
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

int num_work_groups_z_p2 = int(gl_NumWorkGroups.z * gl_NumWorkGroups.z);

int get_index_of_position(in ivec3 position) {
    return int(position.x + position.y * gl_NumWorkGroups.y + position.z * num_work_groups_z_p2);
}

void test_triangle_x(in ivec3 curr_pos, in sSurfacePoint current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_X[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_X[1]);
    // Test first triangle
    if (vertices[t1_v1].is_surface != 0 && vertices[t1_v1].is_surface != 255 && vertices[t1_v2].is_surface != 0 && vertices[t1_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t1_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t1_v2].position, 0.0);
    }

    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_X[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_X[3]);
    // Test first triangle
    if (vertices[t2_v1].is_surface != 0 && vertices[t2_v1].is_surface != 255 && vertices[t2_v2].is_surface != 0 && vertices[t2_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t2_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t2_v2].position, 0.0);
    }
}

void test_triangle_y(in ivec3 curr_pos, in sSurfacePoint current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[1]);
    // Test first triangle
    if (vertices[t1_v1].is_surface != 0 && vertices[t1_v1].is_surface != 255 && vertices[t1_v2].is_surface != 0 && vertices[t1_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t1_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t1_v2].position, 0.0);
    }

    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Y[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Y[3]);
    // Test first triangle
    if (vertices[t2_v1].is_surface != 0 && vertices[t2_v1].is_surface != 255 && vertices[t2_v2].is_surface != 0 && vertices[t2_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t2_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t2_v2].position, 0.0);
    }
}

void test_triangle_z(in ivec3 curr_pos, in sSurfacePoint current_point) {
    int t1_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[0]), t1_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[1]);
    // Test first triangle
    if (vertices[t1_v1].is_surface != 0 && vertices[t1_v1].is_surface != 255 && vertices[t1_v2].is_surface != 0 && vertices[t1_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t1_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t1_v2].position, 0.0);
    }

    int t2_v1 = get_index_of_position(curr_pos + TEST_AXIS_Z[2]), t2_v2 = get_index_of_position(curr_pos + TEST_AXIS_Z[3]);
    // Test first triangle
    if (vertices[t2_v1].is_surface != 0 && vertices[t2_v1].is_surface != 255 && vertices[t2_v2].is_surface != 0 && vertices[t2_v2].is_surface != 255) { // Early out
        int start_vert = atomicAdd(mesh_vertices_count, 3);
        mesh_vertices[start_vert] = vec4(current_point.position, 0.0);
        mesh_vertices[start_vert+1] = vec4(vertices[t2_v1].position, 0.0);
        mesh_vertices[start_vert+2] = vec4(vertices[t2_v2].position, 0.0);
    }
}

void main() {
    ivec3 post = ivec3(gl_GlobalInvocationID);
    int index = get_index_of_position(post);

    sSurfacePoint current_point = vertices[index];

    //atomicAdd(mesh_vertices_count, 1);

    if (current_point.is_surface == 0 || current_point.is_surface == 255) { // Early out
        return;
    }
    
    //int start_vert = atomicAdd(mesh_vertices_count, 3);
    //mesh_vertices[start_vert] = vec4(float(current_point.is_surface), 0.0, 0.0, 0.0);
    //mesh_vertices[start_vert+1] = vec4(float(current_point.is_surface), 0.0, 0.0, 0.0);
    //mesh_vertices[start_vert+2] = vec4(float(current_point.is_surface), 0.0, 0.0, 0.0);
    test_triangle_x(post, current_point);
    test_triangle_y(post, current_point);
    test_triangle_z(post, current_point);
}