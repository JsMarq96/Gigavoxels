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

	// TODO: REVISAR OCTREE THIS SHIT FUCKY
	struct sOctree {
		union {
			uint64_t *raw_octree = NULL;
			sVoxel* octree;
		};
		uint32_t  octree_count = 0;
		uint32_t  SSBO = 0;

		void compute_octree(const char* data_path, 
							const uint16_t width, 
							const uint16_t depth, 
							const uint16_t height) {
			Histopyramid::sPyramid pyramid = {};
			pyramid.init();

			// Load the 3D texture of the volume
			sTexture test_text = {};
			load3D_monochrome(&test_text, data_path, width, height, depth);

			// Compute the histopyramid
			pyramid.compute(test_text, 0.5f);

			// Determine the octree size
			const uint32_t base_32_level_count = 5;
			const uint32_t base_32_total_child_count = get_number_of_children(base_32_level_count);
			const uint32_t octree_level_count = pyramid.num_of_layers - base_32_level_count + 1; // Plus one, since they share 1 layer
			const uint32_t octree_base_level_size = (uint32_t) pow(2.0f, octree_level_count);
			// const uint32_t base_32_number_of_child_blocks = get_number_of_children(base_32_level_count); 
			const uint32_t base_32_number_of_child_blocks = 37448;
			
			// Compute the size of the octree & initilize it
			octree_count = get_number_of_children(octree_level_count+1);
			const uint32_t total_octree_bytesize = octree_count * sizeof(sVoxel);
			octree = (sVoxel*) malloc(total_octree_bytesize);
			memset(octree, 0, total_octree_bytesize);

			// Fill the indices of the octree, using the histopyramid's precomputed ranges
			// If the son_id is 0, its a leaf
			octree[0].brick_id = 2;
			octree[0].son_id = 1;
			uint32_t i = 1;
			// TODO: iterate using the generated indexes of the children, and only fill the importante elements
			//       and making it a sparse octree!
			for(uint32_t curr_level = 1; curr_level < octree_level_count; curr_level++) {
				const uint32_t size = pyramid.pyramid_level_sizes[curr_level] * pyramid.pyramid_level_sizes[curr_level] * pyramid.pyramid_level_sizes[curr_level];
				const uint32_t max_children_count = get_number_of_children(curr_level);

				for(uint32_t j = 0; j < size; j++) {
					// children index = 2^(level + 2) + (in_parent_child_index * 8)
					octree[i].son_id = pow(2.0f, curr_level+2) + (i * 8);

					// Check the histopyramid
					float fill_rate = pyramid.pyramids[i] / (float) max_children_count;
					if (fill_rate < EMPTY_PERCENTAGE) { // Treat it as empty block
						octree[i].brick_id = EMPTY_VOXEL;
					} else if (fill_rate > FULL_PERCENTAGE) { // Treat it as full block
						octree[i].brick_id = FULL_VOXEL;
					} else { // Treat it as mixed block
						octree[i].brick_id = 2;
						// TODO: here there will be a reference to the mipmap of the
						// children blocks.
						// for now, its empty, we use it as ain indicator to iterate
						// and only use it for the leaf elemenents
					}
					i++;
				}
			}

			// Upload octree to SSBO
			glGenBuffers(1, &SSBO);
			glBindBuffer(GL_SHADER_STORAGE_BUFFER, SSBO);
			glBufferData(GL_SHADER_STORAGE_BUFFER, total_octree_bytesize, raw_octree, GL_DYNAMIC_COPY);
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
