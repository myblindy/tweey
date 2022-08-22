#version 460 core

uniform sampler2D lightMapSampler;

in vec2 fs_tex0;

out vec4 color;

void main()
{
    color = texture(lightMapSampler, vec2(fs_tex0.x, 1 - fs_tex0.y));
}