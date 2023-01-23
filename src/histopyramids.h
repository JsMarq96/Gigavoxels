#pragma once

#include <iostream>
#include <cmath>
#include "shader.h"
#include "texture.h"

namespace Histopyramid {

	const char* compute_shader_first_pass = R"(#version 440
layout(std430, binding = 1) buffer pyramid_ssbo {
    uint pyramid[];
};

layout (local_size_x = 2, local_size_y = 2, local_size_z = 2) in;

uniform sampler3D u_texture;
uniform float     u_threshold;
uniform uint      u_pyramid_level_start_index;

void main() {
	// Index = block_id * 2 * in_block_id
	uvec3 texel_index = gl_WorkGroupID.xyz * uvec3(2) + gl_LocalInvocationID.xyz;
	// in_layer_array_index = coord.x + coord.y * height + coord.z * depth * depth
	uint array_index = gl_WorkGroupID.x + (gl_WorkGroupID.y * gl_NumWorkGroups.y)  + gl_WorkGroupID.z * (gl_NumWorkGroups.z * gl_NumWorkGroups.z);
	// store at its apropiate location
	array_index = array_index + u_pyramid_level_start_index;

	float density = texelFetch(u_texture, ivec3(texel_index), 0).r;

	atomicAdd(pyramid[array_index], uint(step(u_threshold, density))); 
})";

	const char* compute_shader_second_pass = R"(#version 440
layout(std430, binding = 1) buffer pyramid_ssbo {
    uint pyramid[];
};

layout (local_size_x = 2, local_size_y = 2, local_size_z = 2) in;

uniform uint      u_pyramid_curr_level_width;
uniform uint      u_pyramid_prev_level_width;
uniform uint      u_pyramid_current_level_start_index;
uniform uint      u_pyramid_prev_level_start_index;
void main() {
	uvec3 child_3D_index = gl_WorkGroupID.xyz * uvec3(2) + gl_LocalInvocationID.xyz;
	uint child_index = child_3D_index.x + child_3D_index.y * u_pyramid_prev_level_width + child_3D_index.z * (u_pyramid_prev_level_width * u_pyramid_prev_level_width);
	child_index = child_index + u_pyramid_prev_level_start_index;

	uint parent_index = gl_WorkGroupID.x + gl_WorkGroupID.y * u_pyramid_curr_level_width + gl_WorkGroupID.z * (u_pyramid_curr_level_width * u_pyramid_curr_level_width);
	parent_index = parent_index + u_pyramid_current_level_start_index;


	atomicAdd(pyramid[parent_index], pyramid[child_index])); 
})";


	struct sPyramid {
		// Compute passes
		sShader  first_pass = {};
		sShader  n_pass;

		// Pyramid data
		uint32_t *pyramids = NULL;
		uint32_t* pyramid_level_start = NULL;
		uint32_t num_of_layers = 0;

		void init() {
			int32_t test[3];
			glGetIntegeri_v(GL_MAX_COMPUTE_WORK_GROUP_COUNT, 0, &test[0]);
			glGetIntegeri_v(GL_MAX_COMPUTE_WORK_GROUP_COUNT, 1, &test[1]);
			glGetIntegeri_v(GL_MAX_COMPUTE_WORK_GROUP_COUNT, 2, &test[2]);
			first_pass.load_compute_shader(compute_shader_first_pass);
		}

		void compute(const sTexture &volumetri_text, const float threshold) {
			uint32_t SSBO;

			// Create storage for the pyramids
			num_of_layers = (uint32_t) log2(volumetri_text.width);

			pyramid_level_start = (uint32_t*) malloc(num_of_layers * sizeof(uint32_t));
			pyramid_level_start[0] = 0;

			uint32_t pyramid_size = 0;
			// For each level, add to the count the size and store it, as the begginig of the next level
			for (uint32_t i = 0, pow = 1; i < num_of_layers; i++) {
				pow *= 2;
				pyramid_size += pow * pow * pow;
				pyramid_level_start[i+1] = pyramid_size;
			}
			uint32_t pyramid_byte_size = pyramid_size * sizeof(uint32_t);
			pyramids = (uint32_t*) malloc(pyramid_byte_size);
			memset(pyramids, 0, pyramid_byte_size);

			glGenBuffers(1, &SSBO);
			glBindBuffer(GL_SHADER_STORAGE_BUFFER, SSBO);
			glBufferData(GL_SHADER_STORAGE_BUFFER, sizeof(pyramids), pyramids, GL_DYNAMIC_COPY);

			first_pass.activate();
			glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 1, SSBO);


			glActiveTexture(GL_TEXTURE0);
			glBindTexture(GL_TEXTURE_3D, volumetri_text.texture_id);

			first_pass.set_uniform_texture("u_texture", GL_TEXTURE0);

			first_pass.set_uniform("u_threshold", threshold);

			first_pass.dispatch(32/2, 32/2, 32/2, true);
			glMemoryBarrier(GL_ALL_BARRIER_BITS);

			uint32_t *data_back = (uint32_t*) glMapBuffer(GL_SHADER_STORAGE_BUFFER, GL_READ_ONLY);
			uint32_t sum = 0;
			for (uint8_t i = 0; i < (32); i++) {
				sum += data_back[i];
			}
			std::cout.flush();

			int i = 0;
		}

	};
}