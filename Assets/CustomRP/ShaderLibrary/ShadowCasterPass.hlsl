#ifndef SRP_SHADER_LIBRARY_SHADOWCASTER_PASS
#define SRP_SHADER_LIBRARY_SHADOWCASTER_PASS

#include "Common.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(real4, _Color)
UNITY_DEFINE_INSTANCED_PROP(real, _CutOff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


struct Attributes
{
    UNITY_VERTEX_INPUT_INSTANCE_ID
    float4 posL : POSITION;
    float2 uv : TEXCOORD;
};

struct Varyings
{
    UNITY_VERTEX_INPUT_INSTANCE_ID
    float4 posH : SV_POSITION;
    float2 uv : TEXCOORD;
};


Varyings vert (Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
 
    output.posH = TransformObjectToHClip(input.posL);
    float4 uv_st = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    output.uv = input.uv * uv_st.xy + uv_st.zw;
    
    return output;
}

void frag (Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    real4 Col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
    
    #if defined(RENDER_MODE_CUTOFF)
    clip(Col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff));
    #endif
}


#endif