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
        size_t    element_size;
        size_t    byte_size;

        inline void bind(const uint32_t location) {
            glBindBufferBase(GL_SHADER_STORAGE_BUFFER, location, SSBO);
        }
    };

    inline void generate_octree_from_3d_texture(const sTexture &volume_texture, 
                                                sGPUOctree *octree_to_fill) {
        // TODO only square textures
        // Compute the total size of the octree
        size_t element_size = 0;
        size_t number_of_levels = log2(volume_texture.depth) / 3;
        uint32_t *levels_start_index = (uint32_t*) malloc(sizeof(uint32_t) * number_of_levels+1);
        for(uint32_t i = 0; i <= number_of_levels; i++) {
            levels_start_index[i] = element_size;
            
            size_t size_of_level = (uint32_t) pow(2, i * 3);
            element_size += size_of_level;
        }

        size_t octree_bytesize = sizeof(sOctreeNode) * element_size;


        // Generate SSBO and the shaders
        uint32_t SSBO;
        glGenBuffers(1, &SSBO);
        glBindBuffer(GL_SHADER_STORAGE_BUFFER, SSBO);
        glBufferData(GL_SHADER_STORAGE_BUFFER, octree_bytesize, NULL, GL_DYNAMIC_COPY);
        glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 2, SSBO);

        sShader compute_first_pass = {}, compute_n_pass = {};
        compute_first_pass.load_file_compute_shader("resources/shaders/octree_load.cs");
        //compute_n_pass.load_file_compute_shader("resources/shaders/octree_generate.cs");

        // First compute pass: Upload the data from the texture
        glActiveTexture(GL_TEXTURE0);
		glBindTexture(GL_TEXTURE_3D, volume_texture.texture_id);

        std::cout << "start at" << levels_start_index[number_of_levels] << std::endl;

        compute_first_pass.activate();
        compute_first_pass.set_uniform_texture("u_volume_map", 0);
        compute_first_pass.set_uniform("u_layer_start", levels_start_index[number_of_levels]);
        compute_first_pass.dispatch(volume_texture.depth/2, 
                                    volume_texture.depth/2, 
                                    volume_texture.depth/2, 
                                    true);
        compute_first_pass.deactivate();

        return;

        for(uint32_t i = number_of_levels-1; i <= 0; i--) {
            compute_n_pass.activate();
            compute_n_pass.set_uniform("u_curr_layer_start", levels_start_index[number_of_levels]);
            compute_n_pass.set_uniform("u_prev_layer_start", levels_start_index[number_of_levels+1]);

            uint32_t curr_size = (uint32_t) pow(2, number_of_levels * 3);
            uint32_t prev_size = (uint32_t) pow(2, (number_of_levels+1) * 3);
            compute_n_pass.set_uniform_vector("u_curr_layer_size", glm::vec3(curr_size, curr_size, curr_size));
            compute_n_pass.set_uniform_vector("u_prev_layer_size", glm::vec3(prev_size, prev_size, prev_size));

            compute_n_pass.dispatch(curr_size/2, 
                                    curr_size/2, 
                                    curr_size/2, 
                                    true);

            compute_n_pass.deactivate();

        }


        free(levels_start_index);
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