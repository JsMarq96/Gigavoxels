#pragma once

#include <iostream>
#include <cmath>
#include <stdint.h>

#include "shader.h"
#include "texture.h"

namespace VolumeCounter {

    static const char* compute_shader_count_pass = R"(#version 440
layout(std430, binding = 1) buffer count_ssbo {
    uint count[];
};

layout (local_size_x = 16, local_size_y = 16, local_size_z = 16) in;

uniform sampler3D u_texture;
uniform float     u_threshold;

void main() {
	// Index = block_id + in_block_id
	uvec3 texel_index = gl_GlobalInvocationID.xyz * uvec3(16u) + gl_LocalInvocationID.xyz;

	uint store_index = gl_GlobalInvocationID.x + (gl_GlobalInvocationID.y * gl_NumWorkGroups.y)  + gl_GlobalInvocationID.z * (gl_NumWorkGroups.z * gl_NumWorkGroups.z);

	float density = texelFetch(u_texture, ivec3(texel_index), 0).r * 2.0;

	atomicAdd(count[store_index], uint(step(u_threshold, density))); 
})";

    // Counts how many ocurrences are in a volume, based on a threshold, in blocks
    // of 16 x 16 x 16
    inline void count_volume_by_threshold(const sTexture &volumetri_text, 
                                          const float threshold, 
                                          uint32_t **count_result, 
                                          uint32_t *count_dims) {
        // TODO: test if texture is square
        sShader count_shader = {};

        // Compile shader
        count_shader.load_compute_shader(compute_shader_count_pass);

        uint32_t dispatch_size = volumetri_text.width / 16;
        *count_dims = dispatch_size;

        // Store space for the results
        const uint32_t result_byte_size = sizeof(uint32_t) * dispatch_size * dispatch_size * dispatch_size;
        *count_result = (uint32_t*) malloc(result_byte_size);
        memset(*count_result, 0, result_byte_size);

        // Generate SSBO where the counting is going to be performed
        uint32_t SSBO;
        glGenBuffers(1, &SSBO);
		glBindBuffer(GL_SHADER_STORAGE_BUFFER, SSBO);
		glBufferData(GL_SHADER_STORAGE_BUFFER, result_byte_size, *count_result, GL_DYNAMIC_COPY);
		glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 1, SSBO);

        //Bind volumetric texture to be counted
        glActiveTexture(GL_TEXTURE0);
		glBindTexture(GL_TEXTURE_3D, volumetri_text.texture_id);

        // Bind the shader & compute the count
        count_shader.activate();
		count_shader.set_uniform_texture("u_texture", GL_TEXTURE0);
		count_shader.set_uniform("u_threshold", threshold);
		count_shader.dispatch(dispatch_size, dispatch_size, dispatch_size, true);
		count_shader.deactivate();

        // Get the count back from GPU memmory
        glGetBufferSubData(GL_SHADER_STORAGE_BUFFER, 0, result_byte_size, *count_result);

        // Clean the SBO
        glDeleteBuffers(1, &SSBO);
    }
}