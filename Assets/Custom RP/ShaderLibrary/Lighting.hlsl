
#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

float3 IncommingLight(Surface surface, Light light) {
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light) {
    return IncommingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surfaceWS, BRDF brdf) {
    ShadowData shadowData = GetShadowData(surfaceWS);
    float3 color = 0.0;

    for (int i=0; i<GetDirectinalLightCount(); i++) {
        color += GetLighting(surfaceWS, brdf, GetDirectionalLight(i, surfaceWS, shadowData));

        // DirectionalShadowData data = GetDirectinalShadowData(i);
        // float3 p = GetDirectionalShadowUV_Debug(data, shadowData, surfaceWS);
        // color += float3(p.x, p.y, p.z);
    }

    return color;
}

#endif // CUSTOM_LIGHTING_INCLUDED