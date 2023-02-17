#ifndef _GIGAVOXEL_H_
#define _GIGAVOXEL_H_
#include <cstdint>
#include <stdint.h>
#include <cmath>

#include "histopyramids.h"
#include "shader.h"

#define FULL_PERCENTAGE   0.95f
#define EMPTY_PERCENTAGE  0.05f
#define EMPTY_VOXEL       0
#define FULL_VOXEL        1
#define HALF_VOXEL        2
namespace Gigavoxel {

	struct sVoxel {
		uint32_t	 son_id;    // If the son_id is 0, the block is a leaf
		uint32_t	 brick_id;  // If brick_id is 0, it is an empty voxel, and if ID is 1, a full one
	};

	// Given the number of children layers, how many total children does it have
	inline uint32_t get_number_of_children(const uint32_t num_of_children_layers) {
		uint32_t count = 0;
		for(uint32_t i = 0, power = 1; i < num_of_children_layers; i++) {
			power *= 2;
			count += power * power * power;
		}

		return count;
	}

	// TODO: REVISAR pyramid THIS SHIT FUCKY
	struct sOctree {
		union {
			uint64_t *raw_octree = NULL;
			sVoxel* octree;
		};

		// Lazy to allocate memmory on runtime
		uint32_t octree_level_start[64];
		uint32_t octree_level_sizes[64];
		uint32_t  octree_size = 0;

		uint32_t  SSBO = 0;

		void compute_octree_from_adress(const char* data_path, 
							const uint16_t width, 
							const uint16_t depth, 
							const uint16_t height) {
			//
			// Load the 3D texture of the volume
			sTexture test_text = {};
			load3D_monochrome(&test_text, data_path, width, height, depth);

			compute_octree(test_text, width, depth, height);
		}

		void compute_octree(const sTexture &volume, 
							const uint16_t width, 
							const uint16_t depth, 
							const uint16_t height) {
			Histopyramid::sPyramid pyramid = {};
			pyramid.init();

			// Compute the histopyramid
			pyramid.compute(volume, 0.20f);

			std::cout << "COUNT: " << pyramid.pyramids[0] << " == " << 128 * 128 * 128<< std::endl;

			const uint32_t base_16_level_count = 4;
			// The size of the octree is the size of the pyramid, but without thelast 4 levels
			const uint32_t octree_layer_count = pyramid.num_of_layers - base_16_level_count;
			const uint32_t octree_base_level_size = pow(2.0f, octree_layer_count);

			// Compute octree size & allocate it ===========
			uint32_t octree_bytesize = 0;
			{
				octree_size = 1;
				// For each level, add to the count the size and store it, as the beginig of the next level
				for (uint32_t i = 1, pow = 1; i <= octree_layer_count; i++) {
					pow *= 2;
					octree_level_start[i] = octree_size;
					octree_level_sizes[i] = pow;
					octree_size += pow * pow * pow;
				}

				octree_bytesize = octree_size * sizeof(sVoxel);
				octree = (sVoxel*) malloc(octree_bytesize);
				memset(octree, 0, octree_bytesize);
			}
	
			// Fill the base layer ================
			{
				const uint32_t axis_size = octree_base_level_size;
				const uint32_t axis_y_stride = axis_size * axis_size;
				const uint32_t layer_size = axis_y_stride * axis_size;

				const uint32_t max_children_count = 16 * 16 * 16;

				// stats for verification
				uint32_t empty = 0;
				uint32_t full = 0;
				uint32_t mix = 0;

				// the indices did not align to expected values!!!
				for(uint32_t i = 0; i < layer_size; i++) {
					uint32_t pyramid_index = pyramid.pyramid_level_start[octree_layer_count] + i;
					uint32_t octree_index = octree_level_start[octree_layer_count] + i;

					float fill_rate = pyramid.pyramids[pyramid_index] / (float) max_children_count;

					octree[octree_index].son_id = 0; // 0 since it is a leaf
					if (fill_rate < EMPTY_PERCENTAGE) { // Treat it as empty block
						octree[octree_index].brick_id = EMPTY_VOXEL;
						empty++;
					} else if (fill_rate > FULL_PERCENTAGE) { // Treat it as full block
						octree[octree_index].brick_id = FULL_VOXEL;
						full++;
					} else { // Treat it as mixed block
						octree[octree_index].brick_id = HALF_VOXEL;
						mix++;
						// TODO: here there will be a reference to the mipmap of the
						// children blocks.
						// for now, its empty, we use it as ain indicator to iterate
						// and only use it for the leaf elemenents
					}
				}
				std::cout << empty << " " << full << " " << mix << std::endl;
			}

			octree[0].son_id = 1;
			octree[0].brick_id = HALF_VOXEL;
			// Build the octree from the ground up
			{
				
				for(uint32_t level = octree_layer_count-1; level > 0; level--) {
					// Level starts
					uint32_t curr_level_start = octree_level_start[level];
					uint32_t prev_level_start = octree_level_start[level+1];

					// Curr level stride & sizes
					uint32_t level_size = octree_level_sizes[level];
					uint32_t level_size_pow_2 = level_size * level_size; // size^2
					uint32_t level_size_pow_3 = level_size_pow_2 * level_size; // size^3

					uint32_t prev_level_size = octree_level_sizes[level+1];
					uint32_t prev_level_size_pow_2 = prev_level_size * prev_level_size; // size^2

					for(uint32_t i = 0; i < level_size_pow_3; i++) {
						// Get current level x,y,z coords, and translate them to
						// the previus layer's coordnates (multiply by 2)
						uint32_t prev_x = (i / level_size_pow_2) * 2;
						uint32_t prev_y = ((i / level_size) % level_size) * 2;
						uint32_t prev_z = (i % level_size) * 2;

						// Get the octree indices for the current value and 
						// the first child (at relative 0,0,0) 
						uint32_t prev_start_index = prev_x + prev_y * prev_level_size + prev_z * prev_level_size_pow_2;
						prev_start_index += prev_level_start;
						uint32_t curr_index = i + curr_level_start;

						// Transalte teh coordinates to the previus layer
						// Itarete on the prev level's children
						bool has_had_fist = false;
						uint32_t curr_type = 0;
						for(uint32_t z = 0; z < 2; z++) {
							// For the index calculation
							uint32_t d_z = z * prev_level_size_pow_2; 
							for(uint32_t y = 0; y < 2; y++) {
								// For the index calculation
								float d_y = y * prev_level_size; 
								for(uint32_t x = 0; x < 2; x++) {
									uint32_t child_index = prev_start_index + x + d_y + d_z;

									if (!has_had_fist) {
										curr_type = octree[child_index].brick_id; 
										has_had_fist = true;
									} else {
										if (curr_type != octree[child_index].brick_id) {
											// The children's type differ, so the result
											// is mixed
											curr_type = HALF_VOXEL;
											goto children_checking_loop_end; // Quick exit out
										}
									}
									// TODO: check if all children are gigavoxel, might need to add a reference to
									// a mip of the values
								}
							}
						}
						children_checking_loop_end:
						// Set the index of the current element's son as the starting index (0,0,0) of the children
						octree[curr_index].son_id = prev_start_index;
						octree[curr_index].brick_id = curr_type;
					}
				}
			}

			// Debugging
			/*octree[1].brick_id = 1;
			octree[2].brick_id = 1;
			octree[3].brick_id = 0;
			octree[4].brick_id = 0;
			octree[5].brick_id = 0;
			octree[6].brick_id = 0;
			octree[7].brick_id = 0;
			octree[8].brick_id = 1;*/

			// Upload octree to SSBO
			glGenBuffers(1, &SSBO);
			glBindBuffer(GL_SHADER_STORAGE_BUFFER, SSBO);
			glBufferData(GL_SHADER_STORAGE_BUFFER, octree_bytesize, raw_octree, GL_DYNAMIC_COPY);
		}

		void bind_gigavoxel(const uint32_t buffer_position) {
			glBindBufferBase(GL_SHADER_STORAGE_BUFFER, buffer_position, SSBO);
		}
		/*
			In-shader iteration(ray_position, ray_it):
				1) Ray-cube intersetion
				2) Get octant of teh intersection
				3) Check the octant ID in the octree
				4) if type == FULL: Return a solid
				5) if type == EMPTY: update the ray_position to the exit point of the octant intersection
				6) if type == MIX: raymarch throught the associated brick
					6.5) if exited && no collision: update the ray_position to the exit point of the octant intersection
				7) If ray_position outside current children, go back one level, until the ray_position is inside the macrovoxel
				8) Go back to 1)

				I susspect that the more challengin part if goint to be the 7
		*/
	};
}


#endif //_GIGAVOXEL_H_
