#ifndef SRP_SHADER_LIBRARY_SHADOW
#define SRP_SHADER_LIBRARY_SHADOW


#define MAX_NUM_DIR_SHADOW 4
#define MAX_NUM_DIR_CASCADE 4
#define SHADOW_SAMPLER sampler_linear_clamp_compare


TEXTURE2D_SHADOW(_DirectioanlShadowAtlas);
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(CustomShaow)
float4x4 _DirectionalShadowMatrixs[MAX_NUM_DIR_SHADOW * MAX_NUM_DIR_CASCADE];
float4 _DirectionalCascadeCullingSpheres[MAX_NUM_DIR_CASCADE];
float _MaxShadowDistance;
float _FadeDistanceRatio;
int _DirectionalCascadeCount;
int _DirCasecadeTileSize;
CBUFFER_END


float FadeShadow(float viewDepth) {
    return saturate((1 - (viewDepth / _MaxShadowDistance)) / _FadeDistanceRatio);
}


float ComputeShadowAttenuation(int shadowTileIndex, float shadowStren, float normalBias, float3 posW, float3 normalW) {
    if (shadowTileIndex < 0)
        return 1;

    float viewDepth = -TransformWorldToView(posW).z;
    if (viewDepth > _MaxShadowDistance)
        return 1;
    
    int cascadeOffset = 0;
    for (; cascadeOffset < _DirectionalCascadeCount; cascadeOffset++) {
        float3 relPos = posW - _DirectionalCascadeCullingSpheres[cascadeOffset].xyz;
        float distance2 = dot(relPos, relPos);
        if (distance2 <= _DirectionalCascadeCullingSpheres[cascadeOffset].w * _DirectionalCascadeCullingSpheres[cascadeOffset].w )
            break;
    }

    if (cascadeOffset == MAX_NUM_DIR_CASCADE)
        return 1;

    shadowStren *= FadeShadow(viewDepth);
    float biasFactor = 2.828226 * _DirectionalCascadeCullingSpheres[cascadeOffset].w / _DirCasecadeTileSize;
    posW += normalW * normalBias * biasFactor;
    float4 posShadowed = mul(_DirectionalShadowMatrixs[shadowTileIndex + cascadeOffset], float4(posW, 1));
    float atten = lerp(1, SAMPLE_TEXTURE2D_SHADOW(_DirectioanlShadowAtlas, SHADOW_SAMPLER, posShadowed), shadowStren);

    return atten;
}



#endif