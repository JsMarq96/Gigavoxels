#pragma once

#include <iostream>
#include <cmath>
#include <stdint.h>

#include "gl3w.h"
#include "glcorearb.h"
#include "shader.h"
#include "texture.h"

// https://github.com/mikolalysenko/mikolalysenko.github.com/blob/master/Isosurface/js/surfacenets.js
namespace SurfaceNets {
    const uint32_t MAX_VERTEX_COUNT = 2000;

    struct sSurfacesPoint {
        glm::vec3 position;
        int is_surface;
        //glm::vec3 normal;
        //float padding2;
    };

    struct sRawMesh {
        glm::vec3 pad;
        int vertices_count = 0;
        sSurfacesPoint vertices[];
    };

    struct sGenerator {
        sRawMesh *vertices;

        sSurfacesPoint* surface_points = NULL;

        uint32_t mesh_area_SSBO = 0;
        uint32_t mesh_vertices_SSBO = 0;

        sShader  mesh_vertex_finder = {};
        sShader  mesh_vertex_generator = {};

        void generate_from_volume(const sTexture &volume_texture, 
                                  const uint32_t sampling_rate) {
            mesh_vertex_finder.load_file_compute_shader("../resources/shaders/surface_find.cs");
            mesh_vertex_generator.load_file_compute_shader("../resources/shaders/surface_triangulize.cs");

            uint32_t max_vertex_count = sampling_rate * sampling_rate * sampling_rate;
            size_t vertices_byte_size = sizeof(sSurfacesPoint) * max_vertex_count;
            size_t mesh_byte_size = sizeof(glm::vec3) * 3 * max_vertex_count;
                        
            uint32_t ssbos[2] = {0,0};
            glGenBuffers(2, ssbos);
            glBindBuffer(GL_SHADER_STORAGE_BUFFER, ssbos[0]);
            // Allocate memory
            glBufferData(GL_SHADER_STORAGE_BUFFER, vertices_byte_size, NULL, GL_DYNAMIC_COPY);
            glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 1, ssbos[0]);

            glActiveTexture(GL_TEXTURE0);
			glBindTexture(GL_TEXTURE_3D, volume_texture.texture_id);

            mesh_vertex_finder.activate();
            mesh_vertex_finder.set_uniform_texture("u_volume_map", 0);
            mesh_vertex_finder.dispatch(sampling_rate, 
                                        sampling_rate,
                                        sampling_rate, 
                                        true);
			mesh_vertex_finder.deactivate();

            glBindBuffer(GL_SHADER_STORAGE_BUFFER, ssbos[1]);
            // Allocate memory
            glBufferData(GL_SHADER_STORAGE_BUFFER, mesh_byte_size + sizeof(uint32_t), NULL, GL_DYNAMIC_COPY);
            glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 1, ssbos[1]);

            mesh_vertex_generator.activate();
            mesh_vertex_generator.dispatch(sampling_rate, 
                                        sampling_rate,
                                        sampling_rate, 
                                        true);
			mesh_vertex_generator.deactivate();

            // Get the vertices back from the GPU memmory
            //surface_points = (sSurfacesPoint*) malloc(vertices_byte_size);
            uint32_t vertex_count = 0;
			glGetBufferSubData(GL_SHADER_STORAGE_BUFFER, 1, sizeof(uint32_t), &vertex_count);
            std::cout <<vertex_count << std::endl;
        }
    };

};