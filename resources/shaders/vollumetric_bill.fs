#version 440

in vec2 v_uv;
in vec3 v_world_position;
in vec3 v_local_position;
in vec2 v_screen_position;

out vec4 o_frag_color;

uniform float u_time;
uniform vec3 u_camera_position;
uniform highp sampler3D u_volume_map;

void main() {
    float depth = textureLod(u_volume_map, v_world_position * 0.5 + 0.5, 0).r;

    if (depth < 0.15) { discard;}

    o_frag_color = vec4(v_world_position * 0.5 + 0.5, 1.0);
}