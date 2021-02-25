#ifndef SRP_SHADER_LIBRARY_COMMON
#define SRP_SHADER_LIBRARY_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/packing.hlsl"

void ClipLOD(real3 posCS, real fadeFactor) {
    #if defined(LOD_FADE_CROSSFADE)
    real dither = InterleavedGradientNoise(posCS.xy, 0);
    clip(fadeFactor + fadeFactor > 0 ? dither : -dither);
    #endif
}


real3 UnpackNormal(real4 sampleNormal, real scale) {
    #if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(sampleNormal, scale);
    #else 
    return UnpackNormalmapRGorAG(sampleNormal, scale); // dxt5 compression
    #endif
}


real3 NormalTangentToWorld(real3 normalTS, real3 normalWS, real3 tangentWS, real sign) {
    real3x3 m = CreateTangentToWorld(normalWS, tangentWS, sign);
    return TransformTangentToWorld(normalTS, m);
}

real ScreenPositionToViewDepth(real4 posScreen) {
    if (unity_OrthoParams.w) { 
        real rawDepth = posScreen.w;
        #if UNITY_REVERSED_Z
            rawDepth = 1 - rawDepth;
        #endif
        //near and far distances are stored in the Y and Z components 
        return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
    }

    return posScreen.w;
}

#endif