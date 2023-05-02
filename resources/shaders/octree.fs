#version 440

in vec3 v_local_position;
in vec3 v_world_position;

layout(location = 0) out vec4 o_frag_color;

uniform vec3 u_camera_position;

struct sOctreeNode {
    uint is_leaf;
    uint child_index;
    vec2 _padding;
};


layout(std430, binding=2) buffer octree_ssbo {
    sOctreeNode octree[];
};


// HELPER FUNCTIONS ================================================
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
}

// Test a point inside an AABB
bool is_inside_AABB(in vec3 point,
                    in vec3 box_origin, 
                    in vec3 box_size) {
    vec3 box_min = box_origin - box_size / 2.0;
    vec3 box_max = box_min + box_size;

    bvec3 test_max = lessThan(point, box_max);
    bvec3 test_min = greaterThan(point, box_min);

    return all(test_max) && all(test_min);
}

// Given a position in a AABB, compute the octant and the 
// relative indexes in the octree
uint get_octant_index_of_pos(in vec3 pos, 
                             in vec3 center, 
                             out vec3 relative_octant) {
    const vec3 relative_pos = normalize(pos - center);// * 0.5 + vec3(1.0);

    const vec3 indices = vec3(1.0, 2.0, 4.0);
    uvec3 comp = uvec3(step(relative_pos, vec3(0.0)) * indices);

    relative_octant = (step(relative_pos, vec3(0.0)) * 2.0 - vec3(1.0));
    //relative_octant = relative_pos;

    return comp.x + comp.y + comp.z;
}

const uint MAX_STEPS = 100u;
const uint NON_LEAF = 0u;
const uint FULL_LEAF = 1u;
const uint EMPTY_LEAF = 2u;

vec3 center_story[MAX_STEPS];
uint index_memory[MAX_STEPS];
vec3 sizes_memory[MAX_STEPS];

uint roll_back_steps(in uint current_index, in vec3 size, in vec3 point) {
    uint index = current_index;

    for (; index > 0u; index--) {
        if (is_inside_AABB(point, center_story[index], sizes_memory[index])) {
            break;
        }

    }

    return max(index, 0u);
}

bool instersection(in vec3 origin, in vec3 size, in vec3 ray_dir, out vec2 t) {
    float t_min = 100.0, t_max = -100.0;
    vec3 box_min = origin - (size / 2.0);
    vec3 box_max = box_min + size;

    // Test first point
    float tmp = dot(box_min, ray_dir);
    t_min = min(t_min, tmp);
    t_max = max(t_max, tmp);

    // Test point
    tmp = dot(box_max, ray_dir);
    t_min = min(t_min, tmp);
    t_max = max(t_max, tmp);

    // Test point
    tmp = dot(vec3(box_min.x, box_min.y, box_max.z), ray_dir);
    t_min = min(t_min, tmp);
    t_max = max(t_max, tmp);

     // Test point
    tmp = dot(vec3(box_min.x, box_max.y, box_min.z), ray_dir);
    t_min = min(t_min, tmp);
    t_max = max(t_max, tmp);

     // Test point
    tmp = dot(vec3(box_min.x, box_max.y, box_max.z), ray_dir);
    t_min = min(t_min, tmp);
    t_max = max(t_max, tmp);

     // Test point
    tmp = dot(vec3(box_max.x, box_min.y, box_min.z), ray_dir);
    t_min = min(t_min, tmp);
    t_max = max(t_max, tmp);

     // Test point
    tmp = dot(vec3(box_max.x, box_min.y, box_max.z), ray_dir);
    t_min = min(t_min, tmp);
    t_max = max(t_max, tmp);

     // Test point
    tmp = dot(vec3(box_max.x, box_max.y, box_min.z), ray_dir);
    t_min = min(t_min, tmp);
    t_max = max(t_max, tmp);

    t = vec2(t_min, t_max);
    return t_min < t_max;
}

vec3 iterate_octree() {
    uint index = 0u;
    vec3 ray_origin = v_world_position;
    vec3 ray_dir = normalize(ray_origin - u_camera_position);
    vec3 near, far, box_origin = vec3(0.0, 0.0, 0.0), box_size = vec3(2.0);

    vec2 t_tmp;
    instersection(box_origin, box_size, ray_dir, t_tmp);
    float t_max = t_tmp.y, t_min = t_tmp.x;
    float tv_min, tv_max;

    return ray_dir * t_min + ray_origin;

    uint i = 0u;
    for(; i <  MAX_STEPS; i++) {
       
    }
    
    return vec3(i / MAX_STEPS);
}



void main() {
    o_frag_color = vec4(iterate_octree(), 1.0);
    return;


    vec3 ray_origin = v_world_position; //(u_model_mat *  vec4(u_camera_eye_local, 1.0)).rgb;
    vec3 ray_dir = normalize(ray_origin - u_camera_position);
    vec3 near, far, box_origin = vec3(0.0, 0.0, 0.0), box_size = vec3(2.0);

    
    ray_AABB_intersection(ray_origin, ray_dir, box_origin, box_size, near, far);
    vec3 r;
    uint index = get_octant_index_of_pos(far * 50.0, box_origin, r);
   
    o_frag_color = vec4(vec3(index/8.0), 1.0);
}