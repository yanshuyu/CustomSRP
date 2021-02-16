#ifndef SRP_SHADER_LIBRARY_META_PASS
#define SRP_SHADER_LIBRARY_META_PASS

#include "Common.hlsl"
#include "SurfaceInput.hlsl"
#include "BRDF.hlsl"


CBUFFER_START(Meta)
bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;
CBUFFER_END

struct Attributes {
    real3 posL : POSITION;
    real2 uv : TEXCOORD;
    real2 lightMapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varying {
    real4 posH : SV_POSITION;
    real2 uv : TEXCOORD;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varying vert(Attributes input) {
    Varying output;
    ZERO_INITIALIZE(Varying, output);
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    input.posL.xy = input.lightMapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw; // transform to light map space
    input.posL.z = input.posL.z > 0.0 ? FLT_MIN : 0.0;
    output.posH = TransformWorldToHClip(real4(input.posL, 1.0));
    output.uv = TransformBaseUV(input.uv);

    return output;
}


real4 frag(Varying input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);
    
    real4 baseCol = GetBaseColor(input.uv);
    #if defined(RENDER_MODE_CUTOFF)
    clip(baseCol.a - GetCutOff(input.uv));
    #endif
    
    real4 meta = 0.0;
    if (unity_MetaFragmentControl.x) { // meta pass for diffuse reflection
        BRDF brdf = GetBRDF(baseCol.rgb, GetMetallic(input.uv), GetSmoothness(input.uv));
        meta.rgb = brdf.diffuse;
        meta.rgb += brdf.specular * brdf.rougness * 0.5; //highly specular but rough materials also pass along some indirect light
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
        meta.a = 1.0;
    }


    return meta;
}



#endif