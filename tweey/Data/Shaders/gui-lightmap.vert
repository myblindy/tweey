#version 460 core

layout(std140) uniform ubo_window
{
    vec2 window_size;
};

layout(location = 0) in vec2 location;
layout(location = 1) in vec4 color;
layout(location = 2) in vec3 tex0;

out vec4 fs_color;
out vec3 fs_tex0;
out vec2 fs_tex1;

void main()
{
    gl_Position = vec4(location / window_size * 2.0 - 1.0, 0.0, 1.0);
    fs_color = color;
    fs_tex0 = tex0;
    fs_tex1 = location / window_size;
}