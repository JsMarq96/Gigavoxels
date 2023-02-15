//
// Created by Juan S. Marquerie on 29/05/2021.
//

#include "texture.h"
#include "gl3w.h"
#include "glcorearb.h"

#define STB_IMAGE_IMPLEMENTATION
#include <stb_image.h>
#include <stb_image_write.h>

void upload_simple_texture_to_GPU(sTexture *text);

#include <iostream>


void load_raw_3D_texture(sTexture* text, uint8_t* raw_data, const uint32_t width, const uint32_t height, const uint32_t depth) {
    text->width = width;
    text->height = height;
    text->depth = depth;

    glGenTextures(1, &text->texture_id);
    glBindTexture(GL_TEXTURE_3D, text->texture_id);

    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_BORDER);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_BORDER);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_BORDER);

    glTexImage3D(GL_TEXTURE_3D,
        0,
        GL_R8,
        width,
        height,
        depth,
        0,
        GL_RED,
        GL_UNSIGNED_BYTE,
        raw_data);

    glGenerateMipmap(GL_TEXTURE_3D);


    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_MAG_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);

    glBindTexture(GL_TEXTURE_3D, 0);
}

void load3D_monochrome(sTexture* text, 
                    const char* texture_name,
                    const uint16_t width_i,
                    const uint16_t heigth_i,
                    const uint16_t depth_i) {
    text->store_on_RAM = false;
    text->width = width_i;
    text->height = heigth_i;
    text->depth = depth_i;
    //text->raw_data = stbi_load(texture_name, &w, &h, &l, 0);

#ifndef __EMSCRIPTEN__
    FILE *file = fopen(texture_name,
                            "rb");

    // Get total filesize
    fseek(file,
          0,
          SEEK_END);
    uint32_t file_size = ftell(file);
    fseek(file,
          0,
          SEEK_SET);

    text->raw_data = (unsigned char*) malloc(file_size + 1);
    //raw_data[file_size] = '\0';

    fread(text->raw_data,
          file_size,
          1,
          file);
    fclose(file);
#else
    raw_data = emscripten_get_preloaded_image_data(texture_name, &w, &h);
    l = 4;
#endif



    assert(text->raw_data != NULL && "Uploading empty texture to GPU");

    glGenTextures(1, &text->texture_id);


    glBindTexture(GL_TEXTURE_3D, text->texture_id);

    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_BORDER);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_BORDER);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_BORDER);

    glTexImage3D(GL_TEXTURE_3D,
                 0,
                 GL_R8,
                 text->width,
                 text->height,
                 text->depth,
                 0,
                 GL_RED,
                 GL_UNSIGNED_BYTE,
                 text->raw_data);

    glGenerateMipmap(GL_TEXTURE_3D);


    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_MAG_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_3D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);

    glBindTexture(GL_TEXTURE_3D, 0);

    //stbi_image_free(text->raw_data);
}

void load_texture(sTexture  *text,
                  const bool is_cube_map,
                  const bool store_on_RAM,
                  const char *texture_name) {
    text->store_on_RAM = store_on_RAM;
    text->is_cube_map = is_cube_map;

    if (is_cube_map) {
        const char *cubemap_terminations[] = {"right.jpg",
                                        "left.jpg",
                                        "top.jpg",
                                        "bottom.jpg",
                                        "front.jpg",
                                        "back.jpg" };

        glGenTextures(1, &text->texture_id);
        glBindTexture(GL_TEXTURE_CUBE_MAP, text->texture_id);

        // Generate buffer based on teh biggest possible size, the bottom terminating png
        char *name_buffer = (char*) malloc(strlen(texture_name) + sizeof("bottom.png") + 1);

        unsigned char* raw_data = NULL;
        for(int i = 0; i < 6; i++) {
            int w, h, l;
            memset(name_buffer, '\0', strlen(texture_name) + sizeof("bottom.png") + 1);
            strcat(name_buffer, texture_name);
            strcat(name_buffer, cubemap_terminations[i]);

            raw_data = stbi_load(name_buffer, &w, &h, &l, 0);

            //info(" Load cubemap: %s %i %i %i", name_buffer, w, h, l);

            glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
                         0,
                         GL_RGB,
                         w,
                         h,
                         0,
                         GL_RGB,
                         GL_UNSIGNED_BYTE,
                         raw_data);
            stbi_image_free(raw_data);
        }

        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);
        glBindTexture(GL_TEXTURE_2D, 0);

        free(name_buffer);

        return;
    }

    int w, h, l;
    text->raw_data = stbi_load(texture_name, &w, &h, &l, 0);

    text->width = w;
    text->height = h;

        assert(text->raw_data != NULL && "Uploading empty texture to GPU");

        glGenTextures(1, &text->texture_id);

        glBindTexture(GL_TEXTURE_2D, text->texture_id);

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

        glTexImage2D(GL_TEXTURE_2D,
                     0,
                     GL_RGBA,
                     w,
                     h,
                     0,
                     GL_RGBA,
                     GL_UNSIGNED_BYTE,
                     text->raw_data);
        glGenerateMipmap(GL_TEXTURE_2D);

        glBindTexture(GL_TEXTURE_2D, 0);

    stbi_image_free(text->raw_data);
}

/*
void store_texture(const sTexture *text,
                  const char *name) {
    stbi_write_png(name,
                   text->width,
                   text->height,
                   text->layers,
                   text->raw_data,
                   text->width * text->layers);
}*/

void upload_simple_texture_to_GPU(sTexture *text) {

    int i;

}


void delete_texture(sTexture  *text) {
    if (text->store_on_RAM) {
        stbi_image_free(text->raw_data);
    }

    glDeleteTextures(1, &text->texture_id);
}
