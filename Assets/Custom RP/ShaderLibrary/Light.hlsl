
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_CustomLight)
    int _DirectinalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
    float3 color;
    float3 direction;
    float attenuation;
};

int GetDirectinalLightCount() {
    return _DirectinalLightCount;
}

DirectionalShaodowsData GetDirectinalShadowData(int index)
{
    DirectionalShaodowsData data;
    data.strength = _DirectionalLightShadowData[index].x;
    data.tileIndex = _DirectionalLightShadowData[index].y;

    return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS) {
    Light light;

    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShaodowsData data = GetDirectinalShadowData(index);
    light.attenuation = GetDirectionalShadowAttenuation(data, surfaceWS);

    return light;
}

#endif //CUSTOM_LIGHT_INCLUDED