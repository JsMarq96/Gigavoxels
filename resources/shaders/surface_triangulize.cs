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

// LUTs for defining the edges & the collision
const int cube_edges_LUT[24] = int[24](
    0, 1, 0, 2, 0, 4, 1, 3, 1, 5, 2, 3, 2, 6, 3, 7, 4, 5, 4, 6, 5, 7, 6, 7
); 
const int edge_table_LUT[256] = int[256](
    0, 7, 25, 30, 98, 101, 123, 124, 168, 175, 177, 182, 202, 205, 211, 212, 772, 771, 797, 794, 870, 865, 895, 888, 940, 939, 949, 946, 974, 969, 983, 976, 1296, 1303, 1289, 1294, 1394, 1397, 1387, 1388, 1464, 1471, 1441, 1446, 1498, 1501, 1475, 1476, 1556, 1555, 1549, 1546, 1654, 1649, 1647, 1640, 1724, 1723, 1701, 1698, 1758, 1753, 1735, 1728, 2624, 2631, 2649, 2654, 2594, 2597, 2619, 2620, 2792, 2799, 2801, 2806, 2698, 2701, 2707, 2708, 2372, 2371, 2397, 2394, 2342, 2337, 2367, 2360, 2540, 2539, 2549, 2546, 2446, 2441, 2455, 2448, 3920, 3927, 3913, 3918, 3890, 3893, 3883, 3884, 4088, 4095, 4065, 4070, 3994, 3997, 3971, 3972, 3156, 3155, 3149, 3146, 3126, 3121, 3119, 3112, 3324, 3323, 3301, 3298, 3230, 3225, 3207, 3200, 3200, 3207, 3225, 3230, 3298, 3301, 3323, 3324, 3112, 3119, 3121, 3126, 3146, 3149, 3155, 3156, 3972, 3971, 3997, 3994, 4070, 4065, 4095, 4088, 3884, 3883, 3893, 3890, 3918, 3913, 3927, 3920, 2448, 2455, 2441, 2446, 2546, 2549, 2539, 2540, 2360, 2367, 2337, 2342, 2394, 2397, 2371, 2372, 2708, 2707, 2701, 2698, 2806, 2801, 2799, 2792, 2620, 2619, 2597, 2594, 2654, 2649, 2631, 2624, 1728, 1735, 1753, 1758, 1698, 1701, 1723, 1724, 1640, 1647, 1649, 1654, 1546, 1549, 1555, 1556, 1476, 1475, 1501, 1498, 1446, 1441, 1471, 1464, 1388, 1387, 1397, 1394, 1294, 1289, 1303, 1296, 976, 983, 969, 974, 946, 949, 939, 940, 888, 895, 865, 870, 794, 797, 771, 772, 212, 211, 205, 202, 182, 177, 175, 168, 124, 123, 101, 98, 30, 25, 7, 0
);

const ivec3 DELTAS[8] = ivec3[8](
    ivec3(0, 0, 0),
    ivec3(0, 0, 1),
    ivec3(0, 1, 0),
    ivec3(0, 1, 1),
    ivec3(1, 0, 0),
    ivec3(1, 0, 1),
    ivec3(1, 1, 0),
    ivec3(1, 1, 1)
);

void main() {
	ivec3 curr_index = ivec3(gl_GlobalInvocationID.xyz);
    vec3 works_size =  vec3(gl_NumWorkGroups.xyz);
    vec3 pos = vec3(curr_index) / works_size;

    int index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * gl_NumWorkGroups.y + gl_GlobalInvocationID.z *  gl_NumWorkGroups.z * gl_NumWorkGroups.z;

    vec3 point = vec3(0.0);
    int axis_seed = 0;
    for(uint i = 0u; i < 8u; i++) {
        float density = texelFetch(u_volume_map, curr_index + DELTAS[i], 0).r;

        if (density > 0.5) {
            point += vec3(DELTAS[i]);
            axis_seed |= 1 << i;
        }
    }
    0xfff

    vec3 value = vec3(0.0);
    if (axis_seed > 0 && axis_count < 8) {
        value = (pos + (point / axis_count) * (1.0/works_size));
    }

    //atomicAdd(vertices_count, 1);
    vertices[index].position = value;
    vertices[index].is_surface = false;
    //vertices[index].normal = vec3(0.0);//gl_GlobalInvocationID.xyz;
    //vertices[index].normal.x = axis_count / 8.0;
}

