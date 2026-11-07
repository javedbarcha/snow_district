Shader "Snowglow/Forest Card Palette" {
 Properties {
  _BaseMap("Forest atlas",2D)="white"{}
  _BaseColor("Tint",Color)=(1,1,1,1)
  _Cutoff("Alpha cutoff",Range(0,1))=.45
  _FoliageGain("Foliage colour match",Vector)=(1,1,1,1)
  _SnowGain("Snow colour match",Vector)=(1,1,1,1)
  _WoodGain("Wood colour match",Vector)=(1,1,1,1)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Pass {
   Name "ForestCardForward"
   Tags {"LightMode"="UniversalForward"}
   Cull Off ZWrite On ZTest LEqual
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    float4 _FoliageGain,_SnowGain,_WoodGain;
    half _Cutoff;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
   Varyings Vert(Attributes input){Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);output.positionCS=TransformObjectToHClip(input.positionOS.xyz);output.uv=TRANSFORM_TEX(input.uv,_BaseMap);output.fog=ComputeFogFactor(output.positionCS.z);return output;}
   half4 Frag(Varyings input):SV_Target{
    half4 atlas=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);clip(atlas.a-_Cutoff);
    #if defined(UNITY_COLORSPACE_GAMMA)
     half foliage=step(atlas.r*1.15h,atlas.g)*step(atlas.b*.8h,atlas.g);
     half wood=(1-foliage)*step(atlas.g*1.12h,atlas.r)*step(atlas.b*1.15h,atlas.r);
    #else
     half foliage=step(atlas.r*1.4h,atlas.g)*step(atlas.b*.6h,atlas.g);
     half wood=(1-foliage)*step(atlas.g*1.3h,atlas.r)*step(atlas.b*1.4h,atlas.r);
    #endif
    half3 gain=lerp(_SnowGain.rgb,_FoliageGain.rgb,foliage);gain=lerp(gain,_WoodGain.rgb,wood);
    return half4(MixFog(atlas.rgb*gain*_BaseColor.rgb,input.fog),1);
   }
   ENDHLSL
  }
 }
}