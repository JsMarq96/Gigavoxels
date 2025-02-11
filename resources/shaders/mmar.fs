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

const int MAX_ITERATIONS = 150;
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

float get_level_of_size(float size) {
    return 1 + log2(size);
}
float get_size_of_miplevel(float level) {
    return round(pow(2.0, level - 1.0));
}

void get_voxel_of_point_in_level(in vec3 point, in float mip_level, out vec3 origin, out vec3 size) {
    float voxel_count_side = get_size_of_miplevel(mip_level);
    vec3 voxel_size = vec3(2.0 / voxel_count_side); // The cube is sized 2,2,2 in world coords
    
    vec3 start_coords = round((point + (voxel_size / 2.0)) / voxel_size) * voxel_size;

    origin = start_coords - voxel_size * 0.5;
    size = voxel_size;
}

bool in_the_same_area(in vec3 p1, in vec3 p2, in float mip_level) {
    float voxel_count_side = get_size_of_miplevel(mip_level);
    vec3 voxel_size = vec3(2.0 / voxel_count_side); // The cube is sized 2,2,2 in world coords
    
    vec3 p1_coords = round((p1 + (voxel_size / 2.0)) / voxel_size) * voxel_size;
    vec3 p2_coords = round((p2 + (voxel_size / 2.0)) / voxel_size) * voxel_size;

    return all(lessThan(p1_coords - p2_coords, vec3(0.001)));
}

float get_distance(in float level) {
    return pow(2.0, level) * SMALLEST_VOXEL * 0.75;
}

void ray_AABB_intersection(in vec3 ray_origin,
                           in vec3 ray_dir,
                           in vec3 box_origin,
                           in vec3 box_size,
                           out vec3 near_intersection,
                           out vec3 far_intersection) {
    vec3 box_min = box_origin - (box_size / 2.0);
    vec3 box_max = box_min + box_size;

    // Testing X axis slab
    float tx1 = (box_min.x - ray_origin.x) / ray_dir.x;
    float tx2 = (box_max.x - ray_origin.x) / ray_dir.x;
    float tmin = min(tx1, tx2);
    float tmax = max(tx1, tx2);

    // Testing Y axis slab
    float ty1 = (box_min.y - ray_origin.y) / ray_dir.y;
    float ty2 = (box_max.y - ray_origin.y) / ray_dir.y;
    tmin = max(min(ty1, ty2), tmin);
    tmax = min(max(ty1, ty2), tmax);

    // Testing Z axis slab
    float tz1 = (box_min.z - ray_origin.z) / ray_dir.z;
    float tz2 = (box_max.z - ray_origin.z) / ray_dir.z;
    tmin = max(min(tz1, tz2), tmin);
    tmax = min(max(tz1, tz2), tmax);

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

vec3 mrm() {
    // Raymarching conf
    vec3 pos = v_world_position;
    vec3 ray_dir = normalize(pos - u_camera_position);
    vec3 it_pos = pos + ray_dir * 0.001;

    // MRM
    uint curr_mipmap_level = 8;
    float dist = 0.001; // Distance from start to sampling point
    const float MAX_DIST = 15.0; // Note, should be the max travel distance of teh ray
    float prev_dist = 0.0;
    vec3 prev_sample_pos = pos;

    vec3 curr_aabb_origin, curr_aabb_size;
    vec3 near, far;
    uint i = 0;

    ray_AABB_intersection(pos, ray_dir, curr_aabb_origin, curr_aabb_size, near, far);
    float dist_max = length(near - far);

    for(; i < MAX_ITERATIONS; i++) {
        vec3 sample_pos = pos + (dist * ray_dir); 
        // Early out, can be skippd

        float depth = textureLod(u_volume_map, sample_pos / 2.0 + 0.5, curr_mipmap_level).r;
        if (depth > 0.0) { // There is a block
            if (curr_mipmap_level == 0) {
                return vec3(1.0);
            }
            curr_mipmap_level--;
            // compute the AABB
            get_voxel_of_point_in_level(sample_pos, 
                                        7 - curr_mipmap_level,
                                        curr_aabb_origin,
                                        curr_aabb_size);

            // Intersect the ray with the AABB
            ray_AABB_intersection(pos, ray_dir, curr_aabb_origin, curr_aabb_size, near, far);

            // Get near pos
            dist = max(length(near - pos) + 0.0001, 0.005);
        } else { // Ray is unblocked
            if (prev_dist < dist && !in_the_same_area(sample_pos, prev_sample_pos, curr_mipmap_level+1)) {
                curr_mipmap_level++;
            } else {
                dist += get_distance(curr_mipmap_level);
            }
        }

        prev_dist = dist;
        prev_sample_pos = sample_pos;
    }

    //return vec3(i / MAX_ITERATIONS);
    return vec3(0.0);
}

vec4 render_volume() {
    vec3 pos = v_world_position /2 + 0.5;
    vec3 ray_dir = normalize(pos - u_camera_position);
    vec3 it_pos = pos - ray_dir * 0.001;
    // Add jitter
    //vec3 jitter_addition = ray_dir * (texture(u_albedo_map, gl_FragCoord.xy / vec2(NOISE_TEX_WIDTH)).rgb * STEP_SIZE);
    //it_pos = it_pos + jitter_addition;
    vec4 final_color = vec4(0.0);
    int i = 0;
    for(; i < MAX_ITERATIONS; i++) {
        if (final_color.a >= 0.95) {
            break;
        }
        
        float depth = texture(u_volume_map, it_pos).r;
        if (0.25 <= depth) {
            return vec4( 1.0);
            return vec4(gradient(it_pos), 1.0);
      }
      it_pos = it_pos + (STEP_SIZE * ray_dir);
   }
   return vec4(vec3(0.0), 1.0);
}
void main() {
    vec3 ray_origin = v_world_position; //(u_model_mat *  vec4(u_camera_eye_local, 1.0)).rgb;
    vec3 ray_dir = normalize(ray_origin - u_camera_position);
   //o_frag_color = texture(u_albedo_map, gl_FragCoord.xy / vec2(NOISE_TEX_WIDTH));
   //o_frag_color = render_volume(); //*
   vec3 near, far, box_origin = vec3(0.0, 0.0, 0.0), box_size = vec3(2.0);

   vec3 origin, size;
   get_voxel_of_point_in_level(v_world_position, 4.0, origin, size);

   ray_AABB_intersection(ray_origin, ray_dir,  origin, size, near, far);

   o_frag_color = vec4(vec3(near), 1.0);
   //o_frag_color = vec4(mrm(), 1.0);   
}