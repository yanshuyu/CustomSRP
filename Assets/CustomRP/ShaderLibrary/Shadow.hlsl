#ifndef SRP_SHADER_LIBRARY_SHADOW
#define SRP_SHADER_LIBRARY_SHADOW

#include "Surface.hlsl"
#include "GI.hlsl"

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


real FadeShadow(real viewDepth) {
    return saturate((1 - (viewDepth / _MaxShadowDistance)) / _FadeDistanceRatio);
}


real GetBakedShadowInChannel(real4 shadowMask, int channel ) {
    #if defined(SHADOW_MASK_DISTANCE) || defined(SHADOW_MASK_ALWAYS)
    if (channel < 0)
        return 1.0;
    return shadowMask[channel];
    #endif

    return 1.0;
}


real ComputeShadowAttenuation(Surface sur, real4 shadowData, GI gi) {
    int shadowTileIndex = (int)shadowData.x;
    real shadowStren = shadowData.y;
    real normalBias = shadowData.z;
    int shadowMaskChannel = (int)shadowData.w;
    real bakedShadow = GetBakedShadowInChannel(gi.bakedShadow, shadowMaskChannel);
    real moduledBakedShadow = lerp(1.0, bakedShadow, shadowStren);
    
    if (shadowTileIndex < 0)
        return moduledBakedShadow;

    real viewDepth = -TransformWorldToView(sur.position).z;
    if (viewDepth > _MaxShadowDistance)
        return moduledBakedShadow;
    
    int cascadeOffset = 0;
    for (; cascadeOffset < _DirectionalCascadeCount; cascadeOffset++) {
        float3 relPos = sur.position - _DirectionalCascadeCullingSpheres[cascadeOffset].xyz;
        float distance2 = dot(relPos, relPos);
        if (distance2 <= _DirectionalCascadeCullingSpheres[cascadeOffset].w * _DirectionalCascadeCullingSpheres[cascadeOffset].w )
            break;
    }

    if (cascadeOffset == MAX_NUM_DIR_CASCADE)
        return moduledBakedShadow;

    real biasFactor = 2.828226 * _DirectionalCascadeCullingSpheres[cascadeOffset].w / _DirCasecadeTileSize;
    sur.position += sur.normal * normalBias * biasFactor;
    real4 posShadowed = mul(_DirectionalShadowMatrixs[shadowTileIndex + cascadeOffset], float4(sur.position, 1));
    real atten = SAMPLE_TEXTURE2D_SHADOW(_DirectioanlShadowAtlas, SHADOW_SAMPLER, posShadowed);
    
    real distanceFadeFactor = FadeShadow(viewDepth);

    #if defined(SHADOW_MASK_DISTANCE)
    atten = lerp(bakedShadow, atten, distanceFadeFactor); // mixing real time shadow and baked shadow
    #elif defined(SHADOW_MASK_ALWAYS)
    atten = lerp(1.0, atten, distanceFadeFactor); // static object no shadow caster(1), while dynamic objects does(0 or 1)
    atten = min(atten, bakedShadow);
    #else
    shadowStren *= distanceFadeFactor;
    #endif

    atten = lerp(1.0, atten, shadowStren); // apply shadow stren
   
    return atten;
}



#endif