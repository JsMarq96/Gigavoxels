#ifndef NET_MESH_RENDERER_H_
#define NET_MESH_RENDERER_H_

#include <cstdint>
#include <glm/glm.hpp>

#include "gl3w.h"
#include "glcorearb.h"
#include "shader.h"
#include "raw_shaders.h"
#include "mesh.h"
#include "material.h"
#include "camera.h"

struct sNetMeshRenderer {
    unsigned int  VAO = 0;
    unsigned int  VBO = 0;
    unsigned int  EBO = 0;

    uint32_t indices_count = 0;

    sMaterial material;

    void config_from_buffers(const uint32_t vertex_buffer, const uint32_t index_count) {
        indices_count = index_count;
        VBO = vertex_buffer;
        
        glGenVertexArrays(1, &VAO);
        glBindVertexArray(VAO);

        // Load vertices
        glBindBuffer(GL_ARRAY_BUFFER, VBO);

        // Vertex position
        glEnableVertexAttribArray(0);
        glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 4 * sizeof(float), (void*) 0);

        glBindVertexArray(0);

        // Load material       
#ifdef _WIN32
        material.add_shader(("..\\resources\\shaders\\basic_vertex.vs"), ("..\\resources\\shaders\\color_fragment.fs"));
#else
         material.add_shader(("../resources/shaders/basic_vertex.vs"), ("../resources/shaders/color_fragment.fs"));
#endif
    }

    void render(const glm::mat4x4 *models,
                const int count,
                const glm::vec3 &camera_position,
                const glm::mat4x4 &view_proj,
                const bool show_wireframe) const {

        glBindVertexArray(VAO);

        if (show_wireframe) {
            glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
        }
        material.enable();

        glDisable(GL_CULL_FACE);

        for(int i = 0; i < count; i++) {
            material.shader.set_uniform_matrix4("u_model_mat", models[i]);
            material.shader.set_uniform_matrix4("u_vp_mat", view_proj);
            material.shader.set_uniform_vector("u_camera_position", camera_position);
            //glDrawElements(GL_TRIANGLES, indices_count, GL_UNSIGNED_INT, 0);
            glDrawArrays(GL_TRIANGLES, 0, indices_count);
        }

        material.disable();
    }

    void clean() {
        glDeleteVertexArrays(1, &VAO);
        glDeleteBuffers(1, &VBO);
        glDeleteBuffers(1, &EBO);
    }
};


#endif
