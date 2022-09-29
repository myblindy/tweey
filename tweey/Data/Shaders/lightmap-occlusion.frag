#version 460 core

uniform sampler2D circleSampler;

in vec2 fs_tex0;

layout (location = 0) out float color;

void main()
{
    color = texture(circleSampler, fs_tex0).r;
}