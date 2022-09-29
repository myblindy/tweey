#version 460 core

uniform sampler2DArray atlasSampler;
uniform sampler2D lightMapSampler;
uniform vec4 ambientColor;

in vec4 fs_color;
in vec3 fs_tex0;
in vec2 fs_tex1;

out vec4 color;

void main()
{
    vec4 light = vec4(texture(lightMapSampler, vec2(fs_tex1.x, 1 - fs_tex1.y)).xyz, 1.0);
    color = fs_color * (texture(atlasSampler, fs_tex0) * max(light, ambientColor) + light * vec4(vec3(0.7), 0.0));
}