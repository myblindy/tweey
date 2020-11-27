#version 460 core

layout(std140) uniform ubo_window
{
    vec2 window_size;
};

in vec2 location;
in vec4 color;

out vec4 fs_color;

void main()
{
    gl_Position = vec4(location / window_size * 2.0 - 1.0, 0.0, 1.0);
    fs_color = color;
}