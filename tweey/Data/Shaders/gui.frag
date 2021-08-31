#version 460 core

uniform sampler2DArray atlasSampler;

in vec4 fs_color;
in vec3 fs_tex0;

out vec4 color;

void main()
{
    color = fs_color * texture(atlasSampler, fs_tex0);
}