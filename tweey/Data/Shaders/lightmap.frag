#version 460 core

struct Light
{
    vec4 location;
    vec4 rangeAndstartColor;
};
const int MaxLightsCount = 16;

layout(std140) uniform ubo_lights
{
    vec4 actualLightCountAndCellSizeAndZero; // lights count, cells per texture, zero
    Light lights[MaxLightsCount];
};

in vec2 fs_tex0;

out vec4 color;

void main()
{
    vec3 resultColor = vec3(0.0);
    const int lightsCount = int(actualLightCountAndCellSizeAndZero.x);
    const vec2 size = actualLightCountAndCellSizeAndZero.yz;
    const vec2 pos = (size - vec2(1.0, 1.0)) * fs_tex0;

    for(int idx = 0; idx < lightsCount; ++idx)
    {
        const float range = lights[idx].rangeAndstartColor.x;
        const vec3 startColor = lights[idx].rangeAndstartColor.yzw;
        const vec2 lightPosition = lights[idx].location.xy;

        float dist = length(lightPosition - pos);
        if(dist <= range)
            resultColor += startColor * ((range - dist) / range);
    }

    color = vec4(resultColor, 1.0);
    //color = color * 0.0001 + vec4(fs_tex0, 0.0, 1.0);
}