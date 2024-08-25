
#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct DirectionalShaodowsData
{
    float strength;
    int tileIndex;
};

float SampleDirectionalShadowAtlas(float3 positionSTS) // shadow texture space
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float GetDirectionalShadowAttenuation(DirectionalShaodowsData data, Surface surfaceWS)
{
    if (data.strength <= 0.0)
        return 1.0;

    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.positionWS, 1.0)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    return lerp(1.0, shadow, data.strength);
}

float3 GetDirectionalShadowUV_Debug(DirectionalShaodowsData data, Surface surfaceWS)
{
    // float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.positionWS, 1.0)).xyz;
    // float shadow = SampleDirectionalShadowAtlas(positionSTS);

    float shadow = GetDirectionalShadowAttenuation(data, surfaceWS);
    return float3(shadow, shadow, shadow);
}

#endif //CUSTOM_SHADOWS_INCLUDED