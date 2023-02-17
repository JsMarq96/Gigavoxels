#version 440

in vec3 v_local_position;
in vec3 v_world_position;

layout(location = 0) out vec4 o_frag_color;

uniform vec3 u_camera_position;

struct sVoxel {
    uint son_id;
    uint brick_id;
};

layout(std430, binding = 2) buffer gigavoxel_ssbo {
    sVoxel voxels[];
};

const float EPSILON = 0.001;
const uint MAX_ITERATIONS = 10u;

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
    /*if (tmin < tmax) {
        near_intersection = ray_dir * tmin + ray_origin;
        far_intersection = ray_dir * tmax + ray_origin;
    } else {
         near_intersection = ray_dir * tmax + ray_origin;
        far_intersection = ray_dir * tmin + ray_origin;
    }*/
   
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
    uvec3 comp = uvec3(step(relative_pos, vec3(EPSILON)) * indices);

    relative_octant = step(relative_pos, vec3(0.0)) * 2.0 - vec3(1.0);
    //relative_octant = relative_pos;

    return comp.x + comp.y + comp.z;
/*
    uint index = 0u;

    if (relative_pos.x > EPSILON) {
        index += 1u;
    }   

    if (relative_pos.y > EPSILON) {
        index += 2u; //  y = 2 * 1
    }

    if (relative_pos.z > EPSILON) {
        index += 4u; // z = 2 * 2 * 1
    }

    return index;*/
}


// ITERATION & RENDERING FUNCTIONS =============================================

// Roll back the history to find a parent layer
// that contains the current point
uint unroll_the_tree(in vec3 point,
                     in uint current_index, 
                     in vec3 center_history[MAX_ITERATIONS],
                     inout vec3 box_center,
                     inout vec3 box_size) {
    for(uint it_index = current_index-2; it_index >= 0; it_index--) {
        box_center = center_history[it_index];
        box_size = box_size * vec3(2.0);

        if (is_inside_AABB(point, 
                           box_center, 
                           box_size)) {
            return it_index;
        }
    }

    // This case should never be reached
    return current_index;
}

// Get a color, given an origin
vec4 iterate_octree(in vec3 ray_origin, 
                    in vec3 ray_dir) {
    vec3 it_ray_pos = ray_origin;
    uint it_octree_index = 0u;

    vec3 box_near_intersection = vec3(0.0);
    vec3 box_far_intersection = vec3(0.0);
    vec3 box_origin = vec3(0.0, 0.0, 0.0), box_size = vec3(2.0);

    uint curr_octant = 0u;
    sVoxel curr_voxel;
    uint iterations_history[MAX_ITERATIONS];
    vec3 center_history[MAX_ITERATIONS];
    uint history_index = 0u;

    for(uint i = 0u; i < MAX_ITERATIONS; i++) {
        // Store an history of indeces in order to unroll the tree
        curr_voxel = voxels[it_octree_index];
        iterations_history[history_index] = it_octree_index;
        center_history[history_index] = box_origin;
        history_index++;

        if (curr_voxel.brick_id == 1u || curr_voxel.son_id == 0u) { // Full voxel or leaf: early out
            break;
        } 

        // Calculate intersection with the current voxel
        ray_AABB_intersection(it_ray_pos, 
                              ray_dir, 
                              box_origin, 
                              box_size, 
                              box_near_intersection, 
                              box_far_intersection);

        
        if (curr_voxel.brick_id == 0u) { // Empty block
            // Push the exit point a bit outside the current voxel
            vec3 exit_point = box_far_intersection + (ray_dir * EPSILON);

            // Find the parent that fits the point 
            history_index = unroll_the_tree(exit_point, 
                                            history_index, 
                                            center_history, 
                                            box_origin, 
                                            box_size);

            // Early out
            if (!is_inside_AABB(exit_point, vec3(0.0), vec3(2.0 - EPSILON))) {
                // it is outside of the main bounding box
                break;
            }

            // Continue from the found point
            it_octree_index = iterations_history[history_index];
            
            // Get octant of the point
            vec3 relative_octant_center;
            curr_octant = get_octant_index_of_pos(exit_point,
                                                  box_origin, 
                                                  relative_octant_center);
        } else { // Mixed voxel
            // Get the octant of the near voxel intersection  & get the son's position
            vec3 relative_octant_center;
            curr_octant = get_octant_index_of_pos(box_near_intersection,
                                                  box_origin, 
                                                  relative_octant_center);
            // each time that we go down a level, we halve the size
            box_size = box_size * vec3(0.5);
            // Move teh box origin to the new voxel
            box_origin = box_origin - (relative_octant_center * (box_size * 0.5));
        }

        // Iterate to the according child
        it_octree_index = voxels[it_octree_index + curr_octant].son_id;
    }

    //return vec4(vec3(history_index/10.0), 1.0);
    if (curr_voxel.brick_id == 0u) {
        return vec4(0.0, 0.0, 0.0, 1.0);
    } else if (curr_voxel.brick_id == 2u) {
        return vec4(0.0, 0.50, 0.0, 1.0);
    }
    return vec4(1.0, 0.0, 0.0, 1.0);
}

vec4 iterate_test(in vec3 ray_origin, 
                    in vec3 ray_dir) {
    vec3 it_ray_pos = ray_origin;
    uint it_octree_index = 1u;

    vec3 box_near_intersection = vec3(0.0);
    vec3 box_far_intersection = vec3(0.0);
    vec3 box_origin = vec3(0.0, 0.0, 0.0), box_size = vec3(2.0);

    uint curr_octant = 0u;
    sVoxel curr_voxel;
    uint iterations_history[MAX_ITERATIONS];
    vec3 center_history[MAX_ITERATIONS];
    uint history_index = 0u;

    curr_voxel = voxels[1u];
            ray_AABB_intersection(it_ray_pos, 
                                  ray_dir, 
                                  box_origin, 
                                  box_size, 
                                  box_near_intersection, 
                                  box_far_intersection);

            vec3 relative_octant_center;
            curr_octant = get_octant_index_of_pos(box_near_intersection,
                                                  box_origin, 
                                                  relative_octant_center);
            // each time that we go down a level, we halve the size
            box_size = box_size * vec3(0.5);
            box_origin = box_origin -(relative_octant_center * (box_size * 0.5));
    curr_voxel = voxels[1u + (curr_octant)];

    return vec4(vec3(curr_voxel.brick_id), 1.0);
    if (curr_voxel.brick_id == 0u) {
        return vec4(0.0, 0.0, 0.0, 1.0);
    } else if (curr_voxel.brick_id == 2u) {
        return vec4(0.0, 0.50, 0.0, 1.0);
    }
    return vec4(1.0, 0.0, 0.0, 1.0);
}

void main() {
    vec3 ray_origin = v_world_position; //(u_model_mat *  vec4(u_camera_eye_local, 1.0)).rgb;
    vec3 ray_dir = normalize(ray_origin - u_camera_position);
    vec3 near, far, box_origin = vec3(0.0, 0.0, 0.0), box_size = vec3(2.0);

    o_frag_color = iterate_octree(ray_origin, ray_dir);

    ray_AABB_intersection(ray_origin, ray_dir, box_origin, box_size, near, far);
    vec3 r;
    uint index = get_octant_index_of_pos(near, box_origin, r);
    return;

    //box_size = box_size * vec3(0.5);
    //box_origin = box_origin - (r * (box_size * 0.5));

    //ray_AABB_intersection(ray_origin, ray_dir, box_origin, box_size, near, far);

    //index = get_octant_index_of_pos(near, box_origin, r);
    vec3 point = far + (ray_dir * 1.2);
    if (is_inside_AABB(point, vec3(0.0, 0.0, 0.0), vec3(2.0))) {
        o_frag_color = vec4( 1.0);
    } else {
         o_frag_color = vec4(0.0);
    }
    //o_frag_color = vec4(ray_dir*0.5 + 1.0, 1.0);//u_color;
}