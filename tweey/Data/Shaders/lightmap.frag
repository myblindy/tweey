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

uniform sampler2D occlusionSampler;

in vec2 fs_tex0;

out vec4 color;

void main()
{
    vec3 resultColor = vec3(0.0);
    const int lightsCount = int(actualLightCountAndCellSizeAndZero.x);
    const vec2 size = actualLightCountAndCellSizeAndZero.yz;
    const vec2 pos = (size - vec2(1.0)) * fs_tex0;

    for(int idx = 0; idx < lightsCount; ++idx)
    {
        const float range = lights[idx].rangeAndstartColor.x;
        const vec3 startColor = lights[idx].rangeAndstartColor.yzw;
        const vec2 lightPosition = lights[idx].location.xy;
        const float dist = length(lightPosition - pos);

        // early abort, the next part is expensive
        if(dist > range)
            continue;

        // walk between pos and lightPosition to find occlusions
        vec2 tempPos = pos;
        const int lineSteps = int(ceil(abs(lightPosition.x - pos.x) > abs(lightPosition.y - pos.y) ? abs(lightPosition.x - pos.x) : abs(lightPosition.y - pos.y)));
        const vec2 lineInc = (lightPosition - pos) / lineSteps;
        bool occluded = false;
        while(length(tempPos - lightPosition) > 0.01)
        {
            const vec2 nextPos = tempPos + lineInc;
            occluded = texture(occlusionSampler, tempPos / (size - vec2(1.0))).x > 0.5;

            if(occluded)
                break;
            
            tempPos = nextPos;
        }

        if(!occluded)
        {
            const float strength = max(0, range - dist) / range;
            resultColor += startColor * strength * strength;
        }
    }

    color = vec4(resultColor, 1.0);
}