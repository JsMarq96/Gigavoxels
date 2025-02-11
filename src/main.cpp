#include <iostream>
#include <GL/gl3w.h>
#include <GLFW/glfw3.h>

#include <glm/glm.hpp>
#include <stdint.h>

#include "glm/gtx/string_cast.hpp"
#include "glm/ext/matrix_clip_space.hpp"
#include "glm/ext/matrix_transform.hpp"
#include "texture.h"
#include "mesh.h"
#include "material.h"
#include "mesh_renderer.h"
#include "shader.h"
#include "input_layer.h"
#include "gigavoxels.h"
#include "volume_counter.h"

#ifdef _WIN32
#include <windows.h>
#endif

// Dear IMGUI
#include "imgui/imgui.h"
#include "imgui/imgui_impl_opengl3.h"
#include "imgui/imgui_impl_glfw.h"

#define WIN_WIDTH	640
#define WIN_HEIGHT	480
#define WIN_NAME	"Test"

#define PI 3.14159265359f

void temp_error_callback(int error_code, const char* descr) {
	std::cout << "GLFW Error: " << error_code << " " << descr << std::endl;
}

// INPUT MOUSE CALLBACk
void key_callback(GLFWwindow *wind, int key, int scancode, int action, int mods) {
	// ESC to close the game
	if (key == GLFW_KEY_ESCAPE && action == GLFW_PRESS) {
		glfwSetWindowShouldClose(wind, GL_TRUE);
	}

	eKeyMaps pressed_key;
	switch(key) {
		case GLFW_KEY_W:
			pressed_key = W_KEY;
			break;
		case GLFW_KEY_A:
			pressed_key = A_KEY;
			break;
		case GLFW_KEY_S:
			pressed_key = S_KEY;
			break;
		case GLFW_KEY_D:
			pressed_key = D_KEY;
			break;
		case GLFW_KEY_UP:
			pressed_key = UP_KEY;
			break;
		case GLFW_KEY_DOWN:
			pressed_key = DOWN_KEY;
			break;
		case GLFW_KEY_RIGHT:
			pressed_key = RIGHT_KEY;
			break;
		case GLFW_KEY_LEFT:
			pressed_key = LEFT_KEY;
			break;

		sInputLayer *input = get_game_input_instance();
		input->keyboard[pressed_key] = (action == GLFW_PRESS) ? KEY_PRESSED : KEY_RELEASED;
	};


}

void mouse_button_callback(GLFWwindow *wind, int button, int action, int mods) {
	char index;

	switch (button) {
	case GLFW_MOUSE_BUTTON_LEFT:
		index = LEFT_CLICK;
		break;

	case GLFW_MOUSE_BUTTON_RIGHT:
		index = RIGHT_CLICK;
		break;

	case GLFW_MOUSE_BUTTON_MIDDLE:
		index = MIDDLE_CLICK;
		break;
	}

	sInputLayer *input = get_game_input_instance();
	input->mouse_clicks[index] = (action == GLFW_PRESS) ? KEY_PRESSED : KEY_RELEASED;
}

void cursor_enter_callback(GLFWwindow *window, int entered) {
	sInputLayer *input = get_game_input_instance();
	input->is_mouse_on_screen = entered;
}

void delete_until(char* string, const char symbol) {
	char* it = string;

	while (*it != '\0') {
		it++;
	}

	while (*it != symbol) {
		it--;
	}

	*it = '\0';
}

char* get_path(const char* local_dir) {
	char* result = (char*) malloc(sizeof(char) * 256);
#ifdef _WIN32 
	GetModuleFileName(NULL, (LPSTR) result, 256);
	delete_until(result, '\\');
	strcat(result, "\\");
	strcat(result, local_dir);
#else
	// TODO
#endif
	return result;
}

glm::vec3 rotate_point(glm::vec3 point, float angle,glm::vec3 center) {
  float s = sin(angle);
  float c = cos(angle);

  // translate point back to origin:
  point.x -= center.x;
  point.z -= center.z;

  // rotate point
  float xnew = point.x * c - point.z * s;
  float ynew = point.x * s + point.z * c;

  // translate point back:
  point.x = xnew + center.x;
  point.z = ynew + center.z;
  return point;
}

void ray_aabb_intersection(const glm::vec3 &ray_origin, const glm::vec3 &ray_dir, const glm::vec3 box_origin, const glm::vec3 box_size, glm::vec3 *nearv, glm::vec3 *farv) {
	glm::vec3 box_min = box_origin;
    glm::vec3 box_max = box_origin + box_size;

    // Testing X axis slab
    float tx1 = (box_min.x - ray_origin.x) / ray_dir.x;
    float tx2 = (box_max.x - ray_origin.x) / ray_dir.x;
    float tmin = glm::min(tx1, tx2);
    float tmax = glm::max(tx1, tx2);

    // Testing Y axis slab
    float ty1 = (box_min.y - ray_origin.y) / ray_dir.y;
    float ty2 = (box_max.y - ray_origin.y) / ray_dir.y;
    tmin = glm::max(glm::min(ty1, ty2), tmin);
    tmax = glm::min(glm::max(ty1, ty2), tmax);
    // Testing Z axis slab
    float tz1 = (box_min.z - ray_origin.z) / ray_dir.z;
    float tz2 = (box_max.z - ray_origin.z) / ray_dir.z;

    tmin = glm::max(glm::min(tz1, tz2), tmin);
    tmax = glm::min(glm::max(tz1, tz2), tmax);

    *nearv = ray_dir * tmin + ray_origin;
    *farv = ray_dir * tmax + ray_origin;
}

void draw_loop(GLFWwindow *window) {
	glfwMakeContextCurrent(window);

	// Complex material cube
	sMeshRenderer cube_renderer;
	sMesh cube_mesh;

	sMaterial cube_material;

	// Test values
	uint8_t *text_data = (uint8_t*) malloc(sizeof(uint8_t) * 128*128*128);
	memset(text_data, 0, sizeof(uint8_t) * 128*128*128);
	for(uint32_t y = 0; y < 64; y++) {
		for(uint32_t x = 0; x < 128; x++) {
			for(uint32_t z = 0; z < 64; z++) {
				text_data[x + y * 128 + z * (128*128)] = 255;
			}
		}
	}

	

	sMaterial octree_material;
	sMaterial raymarching_material;
	sMaterial rrma_raymarching_material;

#ifdef _WIN32
	cube_mesh.load_OBJ_mesh(get_path("resources\\cube.obj"));
	octree_material.add_shader(get_path("..\\resources\\shaders\\basic_vertex.vs"), get_path("..\\resources\\shaders\\gigavoxel_fragment.fs"));
	raymarching_material.add_shader(get_path("..\\resources\\shaders\\basic_vertex.vs"), get_path("..\\resources\\shaders\\mmar.fs"));
	rrma_raymarching_material.add_shader(get_path("..\\resources\\shaders\\basic_vertex.vs"), get_path("..\\resources\\shaders\\raymarching_fragment.fs"));
#else
	cube_mesh.load_OBJ_mesh("resources/cube.obj");
	cube_renderer.material.add_shader(("resources/shaders/basic_vertex.vs"), ("resources/shaders/gigavoxel_fragment.fs"));
#endif
	cube_renderer.create_from_mesh(&cube_mesh);


	double prev_frame_time = glfwGetTime();
	sInputLayer *input_state = get_game_input_instance();

	glm::mat4x4 viewproj_mat = {};

	glm::mat4x4 obj_model = glm::mat4x4(1.0f);
	//obj_model.set_identity();
	//obj_model.set_scale({1.0f, 1.0f, 1.f});

	float camera_angle = 0.01f;
	float camera_height = 5.5f;

#ifdef _WIN32
	const char* volume_tex_dir = get_path("..\\resources\\volumens\\bonsai_256x256x256_uint8.raw");
#else
	const char* volume_tex_dir = "resources/bonsai_256x256x256_uint8.raw";
#endif

	sTexture test_text = {};
	load_raw_3D_texture(&test_text, text_data, 128, 128, 128);
	//load3D_monochrome(&test_text, volume_tex_dir, 256, 256, 256);
	raymarching_material.textures[VOLUME_MAP] = test_text;
	raymarching_material.enabled_textures[VOLUME_MAP] = true;

	rrma_raymarching_material.textures[VOLUME_MAP] = test_text;
	rrma_raymarching_material.enabled_textures[VOLUME_MAP] = true;
	

	//octree_material.add_SSBO(2, octree.SSBO);

	bool raymarch_or_octree = false;

	while(!glfwWindowShouldClose(window)) {
		// Draw loop
		int width, heigth;
		double temp_mouse_x, temp_mouse_y;
		
		glfwGetFramebufferSize(window, &width, &heigth);
		// Set to OpenGL viewport size anc coordinates
		glViewport(0,0, width, heigth);

		float aspect_ratio = (float) width / heigth;

		// OpenGL stuff
		glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
		glClearColor(0.5f, 0.0f, 0.5f, 1.0f);
		glEnable(GL_DEPTH_TEST);

		ImGui_ImplOpenGL3_NewFrame();
    	ImGui_ImplGlfw_NewFrame();
    	ImGui::NewFrame();

		double curr_frame_time = glfwGetTime();
		double elapsed_time = curr_frame_time - prev_frame_time;

		// Mouse position control
		glfwGetCursorPos(window, &temp_mouse_x, &temp_mouse_y);
		input_state->mouse_speed_x = abs(input_state->mouse_pos_x - temp_mouse_x) * elapsed_time;
		input_state->mouse_speed_y = abs(input_state->mouse_pos_y - temp_mouse_y) * elapsed_time;
		input_state->mouse_pos_x = temp_mouse_x;
		input_state->mouse_pos_y = temp_mouse_y;

		// ImGui
		ImGui::Begin("Scene control");

		// Rotate the camera arround
		ImGui::SliderFloat("Camera rotation", &camera_angle, 0.01f, 2.0f * PI);
		ImGui::SliderFloat("Camera height", &camera_height, -25.0f, 20.0f);
		ImGui::Checkbox("Raymarching", &raymarch_or_octree);

		// Config scene
		glm::vec3 camera_original_position = rotate_point(glm::vec3{5.0f, camera_height, 5.0f}, camera_angle, glm::vec3{0.1f, 0.1f, 0.10f});
		//std::cout  << glm::to_string(camera_original_position) << std::endl;
		glm::mat4x4 view_mat = glm::lookAt(camera_original_position, glm::vec3{0.1f, 0.1f, 0.10f},  glm::vec3{0.f, 1.0f, 0.0f});
		glm::mat4x4 projection_mat = glm::perspective(glm::radians(45.0f), (float) WIN_WIDTH / (float) WIN_HEIGHT, 0.1f, 100.0f);

		if (raymarch_or_octree) {
			cube_renderer.material = raymarching_material;
		} else {
			cube_renderer.material = rrma_raymarching_material;
		}
		

		cube_renderer.render(&obj_model, 1, camera_original_position, projection_mat * view_mat, false);

		ImGui::End();

		ImGui::Render();
		ImGui_ImplOpenGL3_RenderDrawData(ImGui::GetDrawData());
		glfwSwapBuffers(window);
		glfwPollEvents();
	}
}

int main() {
	if (!glfwInit()) {
		return EXIT_FAILURE;
	}
	
	// GLFW config
	glfwSetErrorCallback(temp_error_callback);

	glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 4);
	glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 4);
	glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);	
	
	GLFWwindow* window = glfwCreateWindow(WIN_WIDTH, WIN_HEIGHT, WIN_NAME, NULL, NULL);

	glfwSetKeyCallback(window, key_callback);
	glfwSetMouseButtonCallback(window, mouse_button_callback);
	glfwSetCursorEnterCallback(window, cursor_enter_callback);

	glfwMakeContextCurrent(window);
	glfwSwapInterval(1);
	
	if (!window) {
		std::cout << "Error, could not create window" << std::endl; 
	} else {
		if (!gl3wInit()) {
			// IMGUI version
			//IMGUI_CHECKVERSION();
			ImGui::CreateContext();
			ImGuiIO &io = ImGui::GetIO();
			// Platform IMGUI
			ImGui_ImplGlfw_InitForOpenGL(window, true);
			ImGui_ImplOpenGL3_Init("#version 130");
			ImGui::StyleColorsDark();
			draw_loop(window);
		} else {
			std::cout << "Cannot init gl3w" << std::endl;
		}
		
	}

	glfwDestroyWindow(window);
	glfwTerminate();

	return 0;
}
