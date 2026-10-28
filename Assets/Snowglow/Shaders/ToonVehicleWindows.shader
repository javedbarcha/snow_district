Shader "Snowglow/Toon Vehicle Windows"
{
    Properties
    {
        _BaseColor ("Bright cyan", Color) = (0.61, 0.93, 0.97, 1)
        _ReflectionColor ("Painted reflection", Color) = (0.88, 0.99, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        // Imported glazing consists of single planes with mixed facing directions.
        // Both sides are solid; depth writes hide the cab interior.
        Cull Off
        ZWrite On
        Blend One Zero
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ReflectionColor;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; half3 normalOS : NORMAL; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; half3 normalOS : TEXCOORD1; half fog : TEXCOORD2; };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.positionOS = input.positionOS.xyz;
            output.normalOS = input.normalOS;
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }
        ENDHLSL
        Pass
        {
            Name "ToonWindows"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            half4 Frag(Varyings input) : SV_Target
            {
                // Object-space projection works on the combined meshes, including
                // the loader's palette UVs. No texture or reflection probes needed.
                float horizontal = abs(input.normalOS.x) > abs(input.normalOS.z) ? input.positionOS.z : input.positionOS.x;
                float stripe = frac((horizontal - input.positionOS.y * 0.28) * 0.9 + 0.32);
                float aa = max(fwidth(stripe), 0.001);
                half wide = smoothstep(0.08-aa, 0.08+aa, stripe) - smoothstep(0.17-aa, 0.17+aa, stripe);
                half thin = smoothstep(0.23-aa, 0.23+aa, stripe) - smoothstep(0.26-aa, 0.26+aa, stripe);
                half3 color = lerp(_BaseColor.rgb, _ReflectionColor.rgb, saturate(wide + thin));
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
            #pragma vertex Vert
            #pragma fragment DepthFrag
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
