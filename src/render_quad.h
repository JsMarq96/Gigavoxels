#ifndef RENDER_QUAD_H_
#define RENDER_QUAD_H_

#include <GL/gl3w.h>
#include <cstdint>
#include <string.h>
#include <cassert>
#include <stdlib.h>
#include <glm/glm.hpp>

#include "glcorearb.h"
#include "glm/fwd.hpp"
#include "texture.h"
#include "material.h"


static const float quad_geometry[] = {
    -1.0f,  1.0f,  0.0f, 1.0f,
    -1.0f, -1.0f,  0.0f, 0.0f,
     1.0f, -1.0f,  1.0f, 0.0f,

    -1.0f,  1.0f,  0.0f, 1.0f,
    1.0f, -1.0f,  1.0f, 0.0f,
    1.0f,  1.0f,  1.0f, 1.0f
};


// TODO: Add a postprocessing element?
struct sQuadRenderer {
    uint32_t VAO = 0;
    uint32_t VBO = 0;

    sMaterial quad_material;

    void init() {
        glGenVertexArrays(1, &VAO);
        glGenBuffers(1, &VBO);
        glBindVertexArray(VAO);

        glBindBuffer(GL_ARRAY_BUFFER, VBO);
        glBufferData(GL_ARRAY_BUFFER, sizeof(quad_geometry), quad_geometry, GL_STATIC_DRAW);

        glEnableVertexAttribArray(0);
        glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 4 * sizeof(float), (void*) 0);
        glEnableVertexAttribArray(1);
        glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 4 * sizeof(float), (void*) (2 * sizeof(float)));

        glBindVertexArray(0);

        // TODO: Config shader & material
        //quad_material.shader = shader;
    }

    void render(const glm::mat4x4 *models, 
                const uint32_t models_count,
                const glm::mat4x4 &view_proj,
                const glm::vec3 &camera_position) const {

        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);  
        glBindVertexArray(VAO);
        quad_material.enable();

        quad_material.shader.set_uniform_matrix4("u_vp_mat", view_proj);
        quad_material.shader.set_uniform_vector("u_camera_position", camera_position);
        
        for(int i = 0; i < models_count; i++) {
            quad_material.shader.set_uniform_matrix4("u_model_mat", models[i]);
            glDrawArrays(GL_TRIANGLES, 0, 6);
        }

        glDrawArrays(GL_TRIANGLES, 0, 6);

        quad_material.disable();
        glBindVertexArray(0);
        glDisable(GL_BLEND);
    }
};

#endif // RENDER_QUAD_H_
