#ifndef SRP_SHADER_LIBRARY_SURFACE_INPUT
#define SRP_SHADER_LIBRARY_SURFACE_INPUT

#include "Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#if !defined(SURFACE_INPUT_UNLIT)

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_EmissiveTex);

TEXTURE2D(_MaskTex);

TEXTURE2D(_DetailTex);
SAMPLER(sampler_DetailTex);

TEXTURE2D(_NormalTex);
TEXTURE2D(_DetailNormalTex);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(real4, _MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(real4, _DetailTex_ST)
UNITY_DEFINE_INSTANCED_PROP(real4, _Color)
UNITY_DEFINE_INSTANCED_PROP(real4, _Emission)
UNITY_DEFINE_INSTANCED_PROP(real, _CutOff)
UNITY_DEFINE_INSTANCED_PROP(real, _Metallic)
UNITY_DEFINE_INSTANCED_PROP(real, _Smoothness)
UNITY_DEFINE_INSTANCED_PROP(real, _Occllusion)
UNITY_DEFINE_INSTANCED_PROP(real, _Frensel)
UNITY_DEFINE_INSTANCED_PROP(real, _DetailAlbedo)
UNITY_DEFINE_INSTANCED_PROP(real, _DetailSmoothness)
UNITY_DEFINE_INSTANCED_PROP(real, _NormalScale)
UNITY_DEFINE_INSTANCED_PROP(real, _DetailNormalScale)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


real2 TransformBaseUV(real2 uv) {
    real4 baseUV_ST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    return uv * baseUV_ST.xy + baseUV_ST.zw;
}

real2 TransformDetailUV(real2 uv) {
    real4 detailUV_ST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailTex_ST);
    return uv * detailUV_ST.xy + detailUV_ST.zw;
}


real4 GetMask(real2 baseUV) {
    return SAMPLE_TEXTURE2D(_MaskTex, sampler_MainTex, baseUV);
}

real4 GetDetail(real2 detailUV) {
    return SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, detailUV) * 2.0 - 1.0; 
}


real4 GetBaseColor(real2 baseUV, real2 detailUV = 0.0) {
    real4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV); 
    real4 tint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
    real mask = GetMask(baseUV).b;
    real detail = GetDetail(detailUV).r * mask * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailAlbedo);
    albedo.rgb = lerp(albedo.rgb, detail > 0 ? 1.0 : 0.0, abs(detail));
    return albedo * tint;
}

real3 GetEmission(real2 uv) {
    return SAMPLE_TEXTURE2D(_EmissiveTex, sampler_MainTex, uv) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Emission);
}

real GetCutOff(real2 baseUV = 0.0) {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff);
}

real GetMetallic(real2 baseUV = 0.0) {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic) * GetMask(baseUV).r;
}

real GetSmoothness(real2 baseUV = 0.0, real2 detailUV = 0.0) {
    real smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness) * GetMask(baseUV).a;
    real mask = GetMask(baseUV).b;
    real detail = GetDetail(detailUV).b * mask * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailSmoothness);
    return lerp(smoothness, detail > 0 ? 1.0 : 0.0, abs(detail));
}

real GetOccullsion(real2 uv = 0.0) {
    return lerp(1.0, SAMPLE_TEXTURE2D(_MaskTex, sampler_MainTex, uv).g, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Occllusion));
}

real GetFrensel() {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Frensel);
}

real3 GetNormalTS(real2 baseUV, real2 detailUV = 0.0) {
    real4 sampleNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_MainTex, baseUV);
    real scale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale);
    real3 normal = UnpackNormal(sampleNormal, scale);

    real4 sampleDetailNormal = SAMPLE_TEXTURE2D(_DetailNormalTex, sampler_DetailTex, detailUV);
    real detailScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailNormalScale);
    real3 detailNormal = UnpackNormal(sampleDetailNormal, detailScale);

    return BlendNormalRNM(normal, detailNormal);
}

#else 


TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_EmissiveTex);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(real4, _MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(real4, _Color)
UNITY_DEFINE_INSTANCED_PROP(real4, _Emission)
UNITY_DEFINE_INSTANCED_PROP(real, _CutOff)

#if defined(PARTICLE_NEAR_FADE)
UNITY_DEFINE_INSTANCED_PROP(real, _NearFadeDistance)
UNITY_DEFINE_INSTANCED_PROP(real, _NearFadeRange)
#endif

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


real2 TransformBaseUV(real2 uv) {
    real4 baseUV_ST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    return uv * baseUV_ST.xy + baseUV_ST.zw;
}

real4 GetBaseColor(real2 baseUV) {
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
}

real GetCutOff(real2 baseUV = 0.0) {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff);
}

real GetMetallic(real2 baseUV = 0.0) {
    return 0.0;
}


real GetSmoothness(real2 baseUV = 0.0) {
    return 0.0;
}

real GetFrensel() {
    return 0.0;
}

real3 GetEmission(real2 uv) {
    return SAMPLE_TEXTURE2D(_EmissiveTex, sampler_MainTex, uv) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Emission);
}

#if defined(PARTICLE_NEAR_FADE)
real GetParticleNearFadeDistance() {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NearFadeDistance);
}

real GetParticleNearFadeRange() {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NearFadeRange);
}
#endif


#endif // SURFACE_INPUT_UNLIT



#endif //SRP_SHADER_LIBRARY_SURFACE_INPUT