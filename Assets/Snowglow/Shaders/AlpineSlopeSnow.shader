Shader "Snowglow/Alpine Rock and Slope Snow"
{
    Properties
    {
        _SnowColor ("Snow", Color) = (0.98, 0.99, 1, 1)
        _SnowShade ("Snow shade", Color) = (0.83, 0.89, 0.94, 1)
        _RockLight ("Light gray rock", Color) = (0.76, 0.78, 0.80, 1)
        _RockShade ("Gray rock shade (no dark cliffs)", Color) = (0.57, 0.60, 0.63, 1)
        _SnowMaxSlope ("Snow slope limit from horizontal", Range(0, 85)) = 50
        _HighSnowSlope ("High altitude snow slope limit", Range(0, 89)) = 78
        _SnowHeightStart ("Altitude snow starts expanding (world metres)", Float) = 45
        _SnowHeightEnd ("Altitude snow reaches full extent (world metres)", Float) = 110
        _SlopeBlend ("Slope transition degrees", Range(0.5, 12)) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        Blend One Zero
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _SnowColor;
            half4 _SnowShade;
            half4 _RockLight;
            half4 _RockShade;
            half _SnowMaxSlope;
            half _SlopeBlend;
            half _HighSnowSlope;
            float _SnowHeightStart;
            float _SnowHeightEnd;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half fog : TEXCOORD2; half snowMix : TEXCOORD3; };
        Varyings SnowVert(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.fog = ComputeFogFactor(output.positionCS.z);
            half altitude = smoothstep(_SnowHeightStart, max(_SnowHeightStart + 0.01, _SnowHeightEnd), output.positionWS.y);
            half slopeLimit = lerp(_SnowMaxSlope, max(_SnowMaxSlope, _HighSnowSlope), altitude);
            half low = cos(radians(min(89.0h, slopeLimit + _SlopeBlend)));
            half high = cos(radians(max(0.0h, slopeLimit - _SlopeBlend)));
            output.snowMix = smoothstep(low, max(low + 0.0001h, high), normalize(output.normalWS).y);
            return output;
        }
        ENDHLSL
        Pass
        {
            Name "SoftSnow"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SnowVert
            #pragma fragment SnowFrag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            half4 SnowFrag(Varyings input) : SV_Target
            {
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light sun = GetMainLight(shadowCoord);
                half daylight = smoothstep(-0.2h, 0.85h, dot(normalize(input.normalWS), sun.direction));
                half tone = daylight * lerp(0.4h, 1.0h, sun.shadowAttenuation);
                half3 rock = lerp(_RockShade.rgb, _RockLight.rgb, tone);
                half3 snow = lerp(_SnowShade.rgb, _SnowColor.rgb, 0.4h + 0.6h * tone);
                half3 color = lerp(rock, snow, input.snowMix);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex SnowVert
            #pragma fragment DepthFrag
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex SnowShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            Varyings SnowShadowVert(Attributes input)
            {
                Varyings output = SnowVert(input);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction = normalize(_LightPosition - output.positionWS);
                #else
                    float3 direction = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, output.normalWS, direction));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
