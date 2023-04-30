#pragma once

#include <cstddef>
#include <cstdint>
#include <iostream>
#include <cmath>
#include <stdint.h>

#include "gl3w.h"
#include "glcorearb.h"
#include "shader.h"
#include "texture.h"


namespace Octree {

    #define NON_LEAF 0
    #define FULL_LEAF 1
    #define ENPTY_LEAD 2

    struct sOctreeNode {
        uint32_t is_leaf;
        uint32_t child_indexes;
        uint32_t __padding[2];
    };

    struct sGPUOctree {
        uint32_t  SSBO;
        size_t    byte_size;

        inline void bind(const uint32_t location) {
            glBindBufferBase(GL_SHADER_STORAGE_BUFFER, location, SSBO);
        }
    };

    inline void generate_octree_from_3d_texture(const sTexture &texture) {
        
    }


    inline void create_test_octree_two_layers(sGPUOctree *to_fill) {
        size_t octree_node_size = 1 + 8;
        size_t octree_byte_size = octree_node_size * sizeof(sOctreeNode);

        sOctreeNode* octree = (sOctreeNode*) malloc(octree_byte_size);

        // Fill the data
        octree[0].is_leaf = NON_LEAF;
        octree[0].child_indexes = 1;

        for(uint32_t i = 0; i < 8;i++) {
            octree[1 + i].is_leaf = (i % 2) + 1;
            std::cout << 1 + i << " is " << (i % 2) + 1 << std::endl;
        }
        octree[8].is_leaf = FULL_LEAF;

        // Upload to the GPU
        uint32_t SSBO;
        glGenBuffers(1, &SSBO);
        glBindBuffer(GL_SHADER_STORAGE_BUFFER, SSBO);
        glBufferData(GL_SHADER_STORAGE_BUFFER, octree_byte_size, octree, GL_DYNAMIC_COPY);
        glBindBuffer(GL_SHADER_STORAGE_BUFFER, 0);

        free(octree);

        to_fill->SSBO = SSBO;
        to_fill->byte_size = octree_byte_size;
    }

};