#version 460 core

layout(std140) uniform ubo_window
{
    vec2 window_size;
};

layout(location = 0) in vec2 location;
layout(location = 1) in vec2 tex0;

out vec2 fs_tex0;
out vec2 fs_window_size;

void main()
{
    gl_Position = vec4(location, 0.0, 1.0);
    fs_tex0 = tex0;
    fs_window_size = window_size;
}