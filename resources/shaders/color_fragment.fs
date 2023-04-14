#version 330 core

layout(location = 0) out vec4 o_frag_color;

uniform vec4 u_color;
in vec3 v_world_position;

void main() {
    o_frag_color = vec4(v_world_position,1.0);//u_color;
}
