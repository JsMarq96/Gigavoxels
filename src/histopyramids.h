#pragma once

#include <iostream>
#include "shader.h"
#include "texture.h"

namespace Histopyramid {

	const char* compute_shader_first_pass = R"(#version 440
layout(std430, binding = 1) buffer pyramid {
    uint pyramid_count[];
};
layout (local_size_x = 32, local_size_y = 32, local_size_z = 1) in; // nota maximo 32 * 32 = 1024

uniform sampler3D u_texture;
uniform float     u_threshold;

void main() {
	// Index = block_id * 32 * in_block_id
	uvec3 texel_index = gl_WorkGroupID.xyz * uvec3(32) + gl_LocalInvocationID.xyz;
	// array_index = coord.x + coord.y * height + coord.z * depth * depth
	uint array_index = gl_WorkGroupID.x + (gl_WorkGroupID.y * gl_NumWorkGroups.y)  + gl_WorkGroupID.z * (gl_NumWorkGroups.z * gl_NumWorkGroups.z);

	uint count = 0;
	for(uint z = 0; z < 32; z++) {
		uvec3 curr_texel_index = texel_index + uvec3(0,0,z);

		float density = texelFetch(u_texture, ivec3(curr_texel_index), 0).r;

		count = count + uint(step(u_threshold, density)); // step for no branching!
	}

	atomicAdd(pyramid_count[array_index], count); 
})";

	const char* compute_shader_second_pass = R"(#version 440
layout(std430, binding = 1) buffer pyramid {
    uint pyramid_count[];
};
layout (local_size_x = 32, local_size_y = 32, local_size_z = 1) in;

uniform float     u_threshold;

void main() {
	// Index = block_id * 32 * in_block_id
	uvec3 texel_index = gl_WorkGroupID.xyz * uvec3(32) + gl_LocalInvocationID.xyz;
	// array_index = coord.x + coord.y * height + coord.z * depth * depth
	uint array_index = gl_WorkGroupID.x + (gl_WorkGroupID.y * gl_NumWorkGroups.y)  + gl_WorkGroupID.z * (gl_NumWorkGroups.z * gl_NumWorkGroups.z);

	uint count = 0;
	for(uint z = 0; z < 32; z++) {
		uvec3 curr_texel_index = texel_index + uvec3(0,0,z);

		float density = texelFetch(u_texture, ivec3(curr_texel_index), 0).r;

		count = count + uint(step(u_threshold, density)); // step for no branching!
	}

	atomicAdd(pyramid_count[array_index], count); 
})";


	struct sPyramid {
		sShader  first_pass = {};
		sShader  n_pass;

		void init() {
			int32_t test[3];
			glGetIntegeri_v(GL_MAX_COMPUTE_WORK_GROUP_COUNT, 0, &test[0]);
			glGetIntegeri_v(GL_MAX_COMPUTE_WORK_GROUP_COUNT, 1, &test[1]);
			glGetIntegeri_v(GL_MAX_COMPUTE_WORK_GROUP_COUNT, 2, &test[2]);
			first_pass.load_compute_shader(compute_shader_first_pass);
		}

		void compute(const sTexture &volumetri_text, const float threshold) {
			uint32_t SSBO;

			uint32_t pyramids[32];
			memset(pyramids, 0, sizeof(pyramids));

			glGenBuffers(1, &SSBO);
			glBindBuffer(GL_SHADER_STORAGE_BUFFER, SSBO);
			glBufferData(GL_SHADER_STORAGE_BUFFER, sizeof(pyramids), pyramids, GL_DYNAMIC_COPY);

			first_pass.activate();
			glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 1, SSBO);


			glActiveTexture(GL_TEXTURE0);
			glBindTexture(GL_TEXTURE_3D, volumetri_text.texture_id);

			first_pass.set_uniform_texture("u_texture", GL_TEXTURE0);

			first_pass.set_uniform("u_threshold", threshold);
			float dims[2] = {volumetri_text.height, volumetri_text.width};
			first_pass.set_uniform_vector2D("u_height_depth", dims);

			first_pass.dispatch(1, 1, 1, true);
			glMemoryBarrier(GL_ALL_BARRIER_BITS);

			uint32_t *data_back = (uint32_t*) glMapBuffer(GL_SHADER_STORAGE_BUFFER, GL_READ_ONLY);
			uint32_t sum = 0;
			for (uint8_t i = 0; i < 32; i++) {
				sum += data_back[i];
				std::cout << (int32_t)data_back[i] << "heolaa"<< std::endl;
			}
			std::cout.flush();

			int i = 0;
		}

	};
}