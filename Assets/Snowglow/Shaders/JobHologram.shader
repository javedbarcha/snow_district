Shader "Snowglow/Job Hologram" {
 Properties { [HDR] _Tint("Tint",Color)=(0.04,1.2,2.2,0.35) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION;float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0; };
   Varyings Vert(Attributes input){Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);output.uv=input.uv;return output;}
   half4 Frag(Varyings input):SV_Target {
    float fade=1-saturate(input.uv.y);
    float baseGlow=exp(-input.uv.y*12);
    float scan=pow(saturate(sin(input.uv.y*42-_Time.y*2.2)),10);
    float pulse=.88+.12*sin(_Time.y*1.8);
    return half4(_Tint.rgb*half3(1,.45,.75)*(.55+baseGlow+scan*.3),_Tint.a*fade*(.45+baseGlow*.4+scan*.25)*pulse);
   }
   ENDHLSL
  }
 }
}
