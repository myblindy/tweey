#version 460 core

#define PI 3.1415926535897932384626433832795

struct Light
{
    vec4 locationAndAngle;
    vec4 rangeAndstartColor;
};
const int MaxLightsCount = 16;

layout(std140) uniform ubo_lights
{
    Light lights[MaxLightsCount];
};

uniform sampler2D occlusionSampler;

in vec2 fs_tex0;
in vec2 fs_window_size;

out vec4 color;

void main()
{
    vec3 resultColor = vec3(0.0);
    const vec2 size = fs_window_size;
    const vec2 pos = (size - vec2(1.0)) * fs_tex0;

    for(int idx = 0; idx < MaxLightsCount; ++idx)
    {
        const float range = lights[idx].rangeAndstartColor.x;
        const vec2 lightPosition = lights[idx].locationAndAngle.xy;
        const float dist = length(lightPosition - pos);

        const float rawAngle = -atan(lightPosition.x - pos.x, lightPosition.y - pos.y);
        const float angle = mod(rawAngle + PI * 2, PI * 2) / (PI * 2);
        vec2 angleMinMax = lights[idx].locationAndAngle.zw;

        if(angleMinMax.x > angleMinMax.y)
            angleMinMax = vec2(angleMinMax.x, 1.0 + angleMinMax.y);

        // early abort, the next part is expensive
        if(dist > range || !((angle >= angleMinMax.x && angle <= angleMinMax.y) || (angle + 1.0 >= angleMinMax.x && angle + 1.0 <= angleMinMax.y)))
            continue;

        const vec3 startColor = lights[idx].rangeAndstartColor.yzw;

        // walk between pos and lightPosition to find occlusions
        vec2 tempPos = pos;
        const int lineStepSkip = 3;
        int lineSteps = int(ceil(abs(lightPosition.x - pos.x) > abs(lightPosition.y - pos.y) ? abs(lightPosition.x - pos.x) : abs(lightPosition.y - pos.y))) / lineStepSkip;
        const vec2 lineInc = (lightPosition - pos) / lineSteps;

        float lightStrength = 1.0;
        while(lineSteps --> 0)
        {
            const vec2 nextPos = tempPos + lineInc;
            const vec2 occlusionSamplerUV = tempPos / size;
            lightStrength *= 1.0 - texture(occlusionSampler, vec2(occlusionSamplerUV.x, 1 - occlusionSamplerUV.y)).x;

            tempPos = nextPos;
        }

        const float strength = max(0, range - dist) / range ;
        resultColor += startColor * strength * strength * lightStrength;
    }

    color = vec4(resultColor, 1.0);
}