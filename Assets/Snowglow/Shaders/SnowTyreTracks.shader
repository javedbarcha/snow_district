Shader "Snowglow/Snow tyre impressions" {
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
  Pass {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   Offset -1,-1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Input {float4 positionOS:POSITION;half4 color:COLOR;};
   struct Output {float4 positionCS:SV_POSITION;half4 color:COLOR;float fog:TEXCOORD0;};
   Output Vert(Input input){Output o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.color=input.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(Output input):SV_Target{return half4(MixFog(input.color.rgb,input.fog),input.color.a);}
   #pragma multi_compile_fog
   ENDHLSL
  }
 }
}
