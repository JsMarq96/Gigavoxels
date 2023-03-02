#version 330 core

in  vec3 a_pos;
in  vec2 a_uv;
in  vec3 a_normal;

out vec2 v_uv;
out vec3 v_world_position;
out vec3 v_local_position;
out vec2 v_screen_position;

uniform mat4 u_vp_mat;
uniform mat4 u_model_mat;
uniform mat4 u_rotation_mat;

void main() {
    vec4 world_pos = u_model_mat * vec4(a_pos, 1.0);
    v_world_position = world_pos.xyz;
    v_local_position = a_pos;
    gl_Position =  u_vp_mat * world_pos;
    v_uv = a_uv;
    v_screen_position = ((gl_Position.xy / gl_Position.w) + 1.0) / 2.0;
}