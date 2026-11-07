Shader "Snowglow/Cloud Sky Unlit" {
 Properties {
  _BaseMap("Cloud palette",2D)="white"{}
  _BaseColor("Tint",Color)=(1,1,1,1)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Name "CloudForward"
   Tags {"LightMode"="UniversalForward"}
   Cull Back ZWrite On ZTest LEqual
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
   Varyings Vert(Attributes input){
    Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS=TransformObjectToHClip(input.positionOS.xyz);output.uv=TRANSFORM_TEX(input.uv,_BaseMap);return output;
   }
   // Sky clouds retain palette contrast beyond the world's fog range.
   half4 Frag(Varyings input):SV_Target{return half4(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb,1);}
   ENDHLSL
  }
 }
}
