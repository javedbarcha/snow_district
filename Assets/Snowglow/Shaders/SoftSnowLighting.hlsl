#ifndef SNOWGLOW_SOFT_SNOW_LIGHTING
#define SNOWGLOW_SOFT_SNOW_LIGHTING
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
half3 SoftSnowColor(float3 positionWS, half3 normalWS, half3 snow, half3 blueShade)
{
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(positionWS));
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    #endif
    Light sun = GetMainLight(shadowCoord);
    half3 normal = normalize(normalWS);
    half daylight = smoothstep(-0.25h, 0.80h, dot(normal, sun.direction));
    // Reference palette: white upward faces, pale cyan sculpted sides.
    // No specular, reflection probes, grain/normal textures or extra lights.
    half tone = lerp(0.10h, 0.65h, daylight * lerp(0.18h, 1.0h, sun.shadowAttenuation));
    tone = lerp(tone, 1.0h, smoothstep(0.45h, 0.90h, normal.y));
    return lerp(blueShade, snow, tone);
}
#endif
