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

const vec3 DELTA_X = vec3(DELTA, 0.0, 0.0);
const vec3 DELTA_Y = vec3(0.0, DELTA, 0.0);
const vec3 DELTA_Z = vec3(0.0, 0.0, DELTA);

vec3 gradient(in vec3 pos) {
    float x = texture(u_volume_map, pos + DELTA_X).r - texture(u_volume_map, pos - DELTA_X).r;
    float y = texture(u_volume_map, pos + DELTA_Y).r - texture(u_volume_map, pos - DELTA_Y).r;
    float z = texture(u_volume_map, pos + DELTA_Z).r - texture(u_volume_map, pos - DELTA_Z).r;
    return normalize(vec3(x, y, z) / vec3(DELTA * 2.0));
}

vec4 render_volume() {
    vec3 pos = v_world_position;
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
   //o_frag_color = texture(u_albedo_map, gl_FragCoord.xy / vec2(NOISE_TEX_WIDTH));
   o_frag_color = render_volume(); //*
   //o_frag_color = vec4(normalize(v_world_position - u_camera_position), 1.0);
   //o_frag_color = texture(u_frame_color_attachment, v_screen_position);
}