Shader "Snowglow/Cloud Sky Unlit" {
 Properties {
  _BaseMap("Cloud palette",2D)="white"{}
  _BaseColor("Tint",Color)=(1,1,1,1)
  [PerRendererData] _CloudOpacity("Lifecycle opacity",Range(0,1))=1
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass {
   Name "CloudForward"
   Tags {"LightMode"="UniversalForward"}
   Cull Back ZWrite Off ZTest LEqual
   Blend SrcAlpha OneMinusSrcAlpha
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
   UNITY_INSTANCING_BUFFER_START(CloudFade)
    UNITY_DEFINE_INSTANCED_PROP(float,_CloudOpacity)
   UNITY_INSTANCING_BUFFER_END(CloudFade)
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
   Varyings Vert(Attributes input){
    Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_TRANSFER_INSTANCE_ID(input,output);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS=TransformObjectToHClip(input.positionOS.xyz);output.uv=TRANSFORM_TEX(input.uv,_BaseMap);return output;
   }
   // Sky clouds retain palette contrast beyond the world's fog range.
   half4 Frag(Varyings input):SV_Target{UNITY_SETUP_INSTANCE_ID(input);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);half alpha=saturate(UNITY_ACCESS_INSTANCED_PROP(CloudFade,_CloudOpacity))*_BaseColor.a;clip(alpha-.0001h);return half4(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb,alpha);}
   ENDHLSL
  }
 }
}
