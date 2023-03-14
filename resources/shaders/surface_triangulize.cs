#version 440

struct sSurfacePoint {
    vec3 position;
    int is_surface;
};

layout(std430, binding = 1) buffer vertices_surfaces {
    sSurface vertices[];
};

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

uniform highp sampler3D u_volume_map;

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

const ivec3 DELTAS[8] = ivec3[8](
    ivec3(0, 0, 0), // 0
    ivec3(0, 0, 1), // 1  z
    ivec3(0, 1, 0), // 2  y
    ivec3(0, 1, 1), // 3  z y
    ivec3(1, 0, 0), // 4  x
    ivec3(1, 0, 1), // 5  x z
    ivec3(1, 1, 0), // 6  x y
    ivec3(1, 1, 1)  // 7  x y z
);

void main() {
    int index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * gl_NumWorkGroups.y + gl_GlobalInvocationID.z *  gl_NumWorkGroups.z * gl_NumWorkGroups.z;

    sSurfacePoint current_point = vertices[index];

    if (current_point.is_surface == 0 && current_point.is_surface == 0xff) { // Early out
        return;
    }

    vec3 point = vec3(0.0);

    // Test arround X   4 5 6 7: (1 << 4) | (1 << 5) | (1 << 6) | (1 << 7) = 240
    if (current_point.is_surface & 240) { // only check the axis where is
        // Test X
    }

    // Test arround Y  2 3 6 7: (1 << 2) | (1 << 3) | (1 << 6) | (1 << 7)) = 204
    if (current_point.is_surface & 204) { // only check the axis where is
        // Test Y
    }

    // Test arround Z  1 3 5 7: (1 << 1) | (1 << 3) | (1 << 5) | (1 << 7)) = 170
    if (current_point.is_surface & 170) { // only check the axis where is
        // Test Z
    }

    vec3 value = vec3(0.0);
    

    //atomicAdd(vertices_count, 1);
    //vertices[index].position = value;
    //vertices[index].is_surface = false;
    //vertices[index].normal = vec3(0.0);//gl_GlobalInvocationID.xyz;
    //vertices[index].normal.x = axis_count / 8.0;
}

