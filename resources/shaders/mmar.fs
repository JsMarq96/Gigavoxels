#version 440

in vec2 v_uv;
in vec3 v_world_position;
in vec3 v_local_position;
in vec2 v_screen_position;

out vec4 o_frag_color;

uniform float u_time;
uniform vec3 u_camera_position;
uniform highp sampler3D u_volume_map;
//uniform highp sampler2D u_albedo_map; // Noise texture
//uniform highp float u_density_threshold;

const int MAX_ITERATIONS = 100;
const float STEP_SIZE = 0.007; // 0.004 ideal for quality
const int NOISE_TEX_WIDTH = 100;
const float DELTA = 0.003;
const float SMALLEST_VOXEL = 0.0078125; // 2.0 / 256

const vec3 DELTA_X = vec3(DELTA, 0.0, 0.0);
const vec3 DELTA_Y = vec3(0.0, DELTA, 0.0);
const vec3 DELTA_Z = vec3(0.0, 0.0, DELTA);

vec3 gradient(in vec3 pos) {
    float x = texture(u_volume_map, pos + DELTA_X).r - texture(u_volume_map, pos - DELTA_X).r;
    float y = texture(u_volume_map, pos + DELTA_Y).r - texture(u_volume_map, pos - DELTA_Y).r;
    float z = texture(u_volume_map, pos + DELTA_Z).r - texture(u_volume_map, pos - DELTA_Z).r;
    return normalize(vec3(x, y, z) / vec3(DELTA * 2.0));
}

vec3 get_int(in vec3 f) {
    return vec3(ivec3(f));
}

float get_int(in float f) {
    return float(int(f));
}

float get_level_of_size(float size) {
    return 1 + log2(size);
}
float get_size_of_miplevel(float level) {
    return get_int(pow(2.0, level - 1.0));
}

void get_voxel_of_point_in_level(in vec3 point, in float mip_level, out vec3 origin, out vec3 size) {
    float voxel_proportions = 2.0 / get_size_of_miplevel(mip_level);// The cube is sized 2,2,2 in world coords
    vec3 voxel_size = vec3(voxel_proportions); 

    vec3 start_coords = point - mod(point, voxel_proportions);

    origin = start_coords;
    size = voxel_size;
}

bool in_the_same_area(in vec3 p1, in vec3 p2, in float mip_level) {
    float voxel_proportions = 2.0 / get_size_of_miplevel(mip_level);// The cube is sized 2,2,2 in world coords
    vec3 voxel_size = vec3(voxel_proportions); 

    vec3 p1_coords = p1 - mod(p1, voxel_proportions);
    vec3 p2_coords = p2 - mod(p2, voxel_proportions);

    return all(lessThan(p1_coords - p2_coords, vec3(0.001)));
}

float get_distance(in float level) {
    return 2.0 / get_size_of_miplevel(level);
    return pow(2.0, level - 1.0) * SMALLEST_VOXEL * 0.025;
}
//tmin = max(tmin, min(min(t1, t2), tmax));
//tmax = min(tmax, max(max(t1, t2), tmin));
void ray_AABB_intersection(in vec3 ray_origin,
                           in vec3 ray_dir,
                           in vec3 box_origin,
                           in vec3 box_size,
                           out vec3 near_intersection,
                           out vec3 far_intersection) {
    vec3 box_min = box_origin;
    vec3 box_max = box_min + box_size;

    // Testing X axis slab
    float tx1 = (box_min.x - ray_origin.x) / ray_dir.x;
    float tx2 = (box_max.x - ray_origin.x) / ray_dir.x;
    float tmin = min(tx1, tx2);
    float tmax = max(tx1, tx2);

    // Testing Y axis slab
    float ty1 = (box_min.y - ray_origin.y) / ray_dir.y;
    float ty2 = (box_max.y - ray_origin.y) / ray_dir.y;
    tmin = max(tmin, min(min(ty1, ty2), tmax));
    tmax = min(tmax, max(max(ty1, ty2), tmin));

    // Testing Z axis slab
    float tz1 = (box_min.z - ray_origin.z) / ray_dir.z;
    float tz2 = (box_max.z - ray_origin.z) / ray_dir.z;
    tmin = max(tmin, min(min(tz1, tz2), tmax));
    tmax = min(tmax, max(max(tz1, tz2), tmin));

    near_intersection = ray_dir * tmin + ray_origin;
    far_intersection = ray_dir * tmax + ray_origin;
    /*if (tmin < tmax) {
        near_intersection = ray_dir * tmin + ray_origin;
        far_intersection = ray_dir * tmax + ray_origin;
    } else {
         near_intersection = ray_dir * tmax + ray_origin;
        far_intersection = ray_dir * tmin + ray_origin;
    }*/
   
}

void ray_AABB_intersection_v2(in vec3 ray_origin,
                           in vec3 ray_dir,
                           in vec3 box_origin,
                           in vec3 box_size,
                           out vec3 near_intersection,
                           out vec3 far_intersection) {
    vec3 tmax = vec3(0.0), tmin = vec3(10000.0);
    vec3 box_min = box_origin - (box_size / 2.0);
    vec3 box_max = box_min + box_size;
    vec3 inv_dir = 1.0 / ray_dir;

    vec3 t1 = (box_min - ray_origin) * -ray_dir;
    vec3 t2 = (box_max - ray_origin) * -ray_dir;

    tmin = min(max(t1, tmin), max(t2, tmin));
    tmax = max(min(t1, tmax), min(t2, tmax));

    float min_val = min(min(tmin.x, tmin.y), tmin.z);
    float max_val = max(max(tmax.x, tmax.y), tmax.z);

    near_intersection = ray_dir * min_val + ray_origin;
    far_intersection = ray_dir * max_val + ray_origin;
}

vec3 mrm() {
    // Raymarching conf
    vec3 ray_dir = normalize(v_world_position - u_camera_position);
    vec3 pos = v_world_position - ray_dir * 0.001;

    // MRM
    uint curr_mipmap_level = 7;
    float dist = 0.002; // Distance from start to sampling point
    const float MAX_DIST = 15.0; // Note, should be the max travel distance of teh ray
    float prev_dist = 0.0;
    vec3 prev_sample_pos = pos;
    vec3 sample_pos;

    vec3 curr_aabb_origin = vec3(-1.0), curr_aabb_size = vec3(2.0);
    vec3 near, far;

    ray_AABB_intersection(pos, ray_dir, curr_aabb_origin, curr_aabb_size, near, far);
    float dist_max = length(v_world_position - far);

    //return vec3(dist_max);

    uint i = 0;
    for(; i < MAX_ITERATIONS; i++) {
        vec3 sample_pos = pos + (dist * ray_dir); 
        // Early out, can be skippd

        float depth = textureLod(u_volume_map, sample_pos / 2.0 + 0.5, curr_mipmap_level).r;
        if (depth > 0.05) { // There is a block
            //return vec3(1.0, 0.0, 0.0);
            if (curr_mipmap_level == 0) {
                return sample_pos * 0.5 + 0.5;
                break;
                return vec3(1.0);
            }
            curr_mipmap_level--;
            dist = prev_dist;
            sample_pos = prev_sample_pos;
        } else { // Ray is unblocked
            dist += get_distance(curr_mipmap_level);
        }
        prev_dist = dist;
    }

    //return vec3(i / MAX_ITERATIONS);
    //return vec3(sample_pos) * 0.5 + 0.5;
    return vec3(1.0);
}

void main() {
    vec3 ray_origin = v_world_position; //(u_model_mat *  vec4(u_camera_eye_local, 1.0)).rgb;
    vec3 ray_dir = normalize(ray_origin - u_camera_position);
   //o_frag_color = texture(u_albedo_map, gl_FragCoord.xy / vec2(NOISE_TEX_WIDTH));
   //o_frag_color = render_volume(); //*
   vec3 near, far, box_origin = vec3(0.0, 0.0, 0.0), box_size = vec3(2.0);

   vec3 origin, size, pos = ray_origin + ray_dir * 0.001;
   get_voxel_of_point_in_level(pos, 3.0, origin, size);
   ray_AABB_intersection(pos, ray_dir, origin, size, near, far);

   o_frag_color = vec4(vec3(near), 1.0);
   o_frag_color = vec4(mrm(), 1.0);   
}