#ifndef SRP_SHADER_LIBRARY_SHADOW
#define SRP_SHADER_LIBRARY_SHADOW

#include "Surface.hlsl"
#include "GI.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonShadow.hlsl"

#define MAX_NUM_DIR_SHADOW 4
#define MAX_NUM_DIR_CASCADE 4
#define MAX_NUM_POINT_SHADOW 2
#define MAX_NUM_SPOT_SHADOW 16

#define SHADOW_SAMPLER sampler_linear_clamp_compare


TEXTURE2D_SHADOW(_DirectioanlShadowAtlas);

TEXTURE2D_SHADOW(_PointShadowAtlas);

TEXTURE2D_SHADOW(_SpotShadowAtlas);

SAMPLER_CMP(SHADOW_SAMPLER);


CBUFFER_START(CustomShaow)
float4 _ShadowMapSizes;
float _MaxShadowDistance;
float _FadeDistanceRatio;

// diretional
float4x4 _DirectionalShadowMatrixs[MAX_NUM_DIR_SHADOW * MAX_NUM_DIR_CASCADE];
float4 _DirectionalCascadeCullingSpheres[MAX_NUM_DIR_CASCADE];
int _DirectionalCascadeCount;

// point
float4x4 _PointShadowMatrixs[MAX_NUM_POINT_SHADOW * 6];

// spot 
float4x4 _SpotShadowMatrixs[MAX_NUM_SPOT_SHADOW];
float4 _SpotShadowTileViewPorts[MAX_NUM_SPOT_SHADOW];
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


real GetDirectionalShadowAtten(real4 shadowData, Surface sur, Light light, GI gi) {
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

    sur.position += GetShadowPosOffset(dot(sur.normal, light.direction), sur.normal, real2(1 / _ShadowMapSizes.x, 1 / _ShadowMapSizes.x)) * normalBias * _DirectionalCascadeCullingSpheres[cascadeOffset].w;
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


real GetPointShadowAtten(real4 shadowData, Surface sur, Light light) {   
    int tileIdx = shadowData.x;
    real shadowStren = shadowData.y;
    real normalBias = shadowData.z;

    if (tileIdx < 0) // not cast shadow
        return 1;
    
    real viewDepth = -TransformWorldToView(sur.position).z;
    if (viewDepth > _MaxShadowDistance)
        return 1.0;
    
    sur.position += GetShadowPosOffset(dot(sur.normal, light.direction), sur.normal, real2(1 / _ShadowMapSizes.y, 1 / _ShadowMapSizes.y)) * normalBias;
    int faceOffset = CubeMapFaceID(-light.direction);
    real4 posShadowed = mul(_PointShadowMatrixs[tileIdx + faceOffset], real4(sur.position, 1));
    posShadowed /= posShadowed.w;
    
    real fadeFactor = FadeShadow(viewDepth);
    real atten = SAMPLE_TEXTURE2D_SHADOW(_PointShadowAtlas, SHADOW_SAMPLER, posShadowed);
    return lerp(1, atten, shadowStren * fadeFactor);
}


real GetSpotShadowAtten(real4 shadowData, Surface sur, Light light) {
    int tileIdx = shadowData.x;
    real shadowStren = shadowData.y;
    real normalBias = shadowData.z;

    if (tileIdx < 0)
        return 1.0;

    real viewDepth = -TransformWorldToView(sur.position).z;
    if (viewDepth > _MaxShadowDistance)
        return 1.0;

    sur.position += GetShadowPosOffset(dot(sur.normal, light.direction), sur.normal, real2(1 / _ShadowMapSizes.z, 1 / _ShadowMapSizes.z)) * normalBias;
    real4 posShadowed = mul(_SpotShadowMatrixs[tileIdx], real4(sur.position, 1));
    posShadowed /= posShadowed.w;
    posShadowed.xy = clamp(posShadowed.xy, _SpotShadowTileViewPorts[tileIdx].xy, _SpotShadowTileViewPorts[tileIdx].xy + _SpotShadowTileViewPorts[tileIdx].zw);

    real fadeFactor = FadeShadow(viewDepth);
    real atten = SAMPLE_TEXTURE2D_SHADOW(_SpotShadowAtlas, SHADOW_SAMPLER, posShadowed);
    return lerp(1, atten, shadowStren * fadeFactor );
}



#endif