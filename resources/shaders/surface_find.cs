#version 440

layout(std430, binding = 1) buffer vertices_surfaces {
    vec4 vertices[];
};

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

uniform highp sampler3D u_volume_map;

const vec3 DELTAS[8] = vec3[8](
    vec3(0,0,0),
    vec3(1,0,0), // 
    vec3(0,1,0),
    vec3(1,1,0),
    vec3(0,0,1),
    vec3(1,0,1),
    vec3(0,1,1),
    vec3(1,1,1)
);

const vec3 DELTASU[7] = vec3[7](
    vec3(0,0,0), // 0
    vec3(0,1,0), // 1 0b01
    vec3(0,-1,0), // 2 0b10
    vec3(1,0,0),  // 3 0b100
    vec3(-1,0,0),  // 4 0b1000
    vec3(0,0,-1), // 5 0b10000
    vec3(0,0,1)  // 6  0b100000
);

ivec3 num_work_groups = ivec3(gl_NumWorkGroups.xyz);

int num_work_groups_z_p2 = (num_work_groups.z * num_work_groups.z);

int get_index_of_position(in ivec3 position) {
    return int(position.x + position.y * num_work_groups.y + position.z * num_work_groups_z_p2);
}

void main() {
	ivec3 curr_index = ivec3(gl_GlobalInvocationID.xyz);
    vec3 works_size =  vec3(gl_NumWorkGroups.xyz);
    vec3 world_position = vec3(curr_index) / works_size;
    ivec3 pos = ivec3(world_position * 256.0);

    uint index = get_index_of_position(curr_index);

    vec3 point = vec3(0.0);
    int axis_seed = 0;
    int axis_count = 0;
    for(int i = 0; i < 8; i++) {
        vec3 delta_pos = (world_position + (DELTAS[i])/works_size);
        float density = texture(u_volume_map, delta_pos).r;

        if (density > 0.25) {
            point += delta_pos;
            axis_seed |= 1 << i;
            axis_count++;
        }
    }

    vec3 value = vec3(0.0);
    if (axis_count > 0 && axis_count < 8) {
        value = ((point / axis_count));
    } else {
        axis_seed = 0;
    }

    //atomicAdd(vertices_count, 1);
    vertices[index] = vec4(value, float(axis_seed));//(vec3(1));
    //vertices[index].is_surface = 1;
    //vertices[index].normal = vec3(0.0);//gl_GlobalInvocationID.xyz;
    //vertices[index].normal.x = axis_count / 8.0;
}

