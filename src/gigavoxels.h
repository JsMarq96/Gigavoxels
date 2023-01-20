#ifndef _GIGAVOXEL_H_
#define _GIGAVOXEL_H_
#include <cstdint>

namespace Gigavoxel {

	struct sVoxel {
		uint32_t	 son_id;
		uint32_t	 brick_id; // If brick ID is 0, it is an empty
	};

	struct sOctree {
		union {
			uint64_t *raw_octree = NULL;
			sVoxel* octree;
		};
		uint32_t  octree_count = 0;

		void compute_octree(const uint8_t* raw_data, 
							const uint16_t width, 
							const uint16_t depth, 
							const uint16_t height) {
			// Iterar ground up
			// generar histopiramides
		}
	};
}


#endif //_GIGAVOXEL_H_
