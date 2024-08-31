
#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4 _ShadowDistanceFade;
CBUFFER_END

struct DirectionalShadowData
{
    float strength;
    int tileIndex;
};

struct ShadowData
{
    int cascadeIndex;
    float strength;
};

float SampleDirectionalShadowAtlas(float3 positionSTS) // shadow texture space
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
    if (data.strength <= 0.0)
        return 1.0;

    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.positionWS, 1.0)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    return lerp(1.0, shadow, data.strength);
}

float FadeShadowStrength(float depth, float maxShadowDistanceRcp, float fadeRcp)
{
    return saturate((1.0 - depth * maxShadowDistanceRcp) * fadeRcp);
}

ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.strength = FadeShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);

    int i;
    for (i = 0; i<_CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqrt = DistanceSquared(surfaceWS.positionWS, sphere.xyz);
        if (distanceSqrt < sphere.w)
        {
            if (i == _CascadeCount - 1)
            {
                data.strength *= FadeShadowStrength(distanceSqrt, 1.0 / sphere.w, _ShadowDistanceFade.z);
            }

            break;
        }
    }

    if (i == _CascadeCount)
    {
        data.strength = 0.0;
    }
    data.cascadeIndex = i;
    return data;
}

float3 GetDirectionalShadowUV_Debug(DirectionalShadowData data, Surface surfaceWS)
{
    // float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.positionWS, 1.0)).xyz;
    // float shadow = SampleDirectionalShadowAtlas(positionSTS);

    float shadow = GetDirectionalShadowAttenuation(data, surfaceWS);
    return float3(shadow, shadow, shadow);
}

#endif //CUSTOM_SHADOWS_INCLUDED