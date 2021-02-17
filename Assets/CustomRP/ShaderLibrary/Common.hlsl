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


void ClipLOD(real3 posCS, real fadeFactor) {
    #if defined(LOD_FADE_CROSSFADE)
    real dither = InterleavedGradientNoise(posCS.xy, 0);
    clip(fadeFactor + fadeFactor > 0 ? dither : -dither);
    #endif
}

#endif