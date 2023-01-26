#ifndef _GIGAVOXEL_H_
#define _GIGAVOXEL_H_
#include <cstdint>
#include <processthreadsapi.h>
#include <stdint.h>
#include "histopyramids.h"

namespace Gigavoxel {

	struct sVoxel {
		uint32_t	 son_id;
		uint32_t	 brick_id; // If brick ID is 0, it is an empty
	};

	uint32_t get_number_of_children(const uint32_t num_of_children_layers) {
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
			// Iterar ground up
			// Generate Histopyramids
			Histopyramid::sPyramid pyramid = {};
			pyramid.init();

			sTexture test_text = {};
			load3D_monochrome(&test_text, data_path, 256, 256, 256);

			pyramid.compute(test_text, 0.5f);
			
			// Need the results of the 32^3 blocks, so 2^5
			const uint32_t desired_level = 5;
			const uint32_t total_number_of_child_blocks = get_number_of_children(pyramid.num_of_layers - desired_level);  
			uint32_t empty_blocks = 0;
			uint32_t full_blocks = 0;
			uint32_t half_blocks = 0;
			for(uint32_t x = 0; x < 32; x++) { // NOTE ups chace
				for(uint32_t y = 0; y < 32; y++) {
					for(uint32_t z = 0; z < 32; z++) {
						uint32_t count = pyramid.get_value_at(desired_level, x, y, z);
						
						float proportion = count / (float) total_number_of_child_blocks;

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

			std::cout << "Half blocks: " << half_blocks << std::endl;
			std::cout << "Full blocks: " << full_blocks << std::endl;
			std::cout << "Empty blocks: " << empty_blocks << std::endl;
		}
	};
}


#endif //_GIGAVOXEL_H_
