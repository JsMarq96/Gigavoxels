#ifndef _GIGAVOXEL_H_
#define _GIGAVOXEL_H_
#include <cstdint>
#include <stdint.h>
#include <cmath>

#include "histopyramids.h"

namespace Gigavoxel {

	struct sVoxel {
		uint32_t	 son_id;    // If the son_id is 0, the block is a leaf
		uint32_t	 brick_id;  // If brick_id is 0, it is an empty voxel, and if ID is 1, a full one
	};

	inline uint32_t get_number_of_children(const uint32_t num_of_children_layers) {
		uint32_t count = 0;
		for(uint32_t i = 0, power = 1; i < num_of_children_layers; i++) {
			power *= 2;
			count += power * power * power;
		}

		return count;
	}

	struct sOctree {
		union {
			uint64_t *raw_octree = NULL;
			sVoxel* octree;
		};
		uint32_t  octree_count = 0;

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
			octree[0].son_id = 1;
			for(uint32_t curr_level = 1, i = 1; curr_level < octree_level_count; curr_level++) {
				uint32_t size = pyramid.pyramid_level_sizes[curr_level] * pyramid.pyramid_level_sizes[curr_level] * pyramid.pyramid_level_sizes[curr_level];
				for(uint32_t j = 0; j < size; j++) {
					octree[i].son_id = pow(2.0f, curr_level+2) + (i * 8);
					i++;
				}
			}


			uint32_t empty_blocks = 0;
			uint32_t full_blocks = 0;
			uint32_t half_blocks = 0;
			for(uint32_t z = 0; z < octree_base_level_size; z++) { // NOTE is the cache ordering correct?
				for(uint32_t y = 0; y < octree_base_level_size; y++) {
					for(uint32_t x = 0; x < octree_base_level_size; x++) {
						uint32_t count = pyramid.get_value_at(octree_level_count, x, y, z);
						
						float proportion = count / (float) base_32_total_child_count;

						if (proportion < 0.15f) {
							empty_blocks++;
						} else if (proportion > 0.85f) {
							full_blocks++;
						} else {
							half_blocks++;
						}
					}
				}
			}

			std::cout << "Total blocks: " << empty_blocks + full_blocks + half_blocks << std::endl;
			std::cout << "Half blocks: " << half_blocks << std::endl;
			std::cout << "Full blocks: " << full_blocks << std::endl;
			std::cout << "Empty blocks: " << empty_blocks << std::endl;
		}
	};
}


#endif //_GIGAVOXEL_H_
