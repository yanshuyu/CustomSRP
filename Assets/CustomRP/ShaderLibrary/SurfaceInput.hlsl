#ifndef SRP_SHADER_LIBRARY_SURFACE_INPUT
#define SRP_SHADER_LIBRARY_SURFACE_INPUT


#ifndef SURFACE_INPUT_UNLIT

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_EmissiveTex);

TEXTURE2D(_MaskTex);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(real4, _MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(real4, _Color)
UNITY_DEFINE_INSTANCED_PROP(real4, _Emission)
UNITY_DEFINE_INSTANCED_PROP(real, _CutOff)
UNITY_DEFINE_INSTANCED_PROP(real, _Metallic)
UNITY_DEFINE_INSTANCED_PROP(real, _Smoothness)
UNITY_DEFINE_INSTANCED_PROP(real, _Occllusion)
UNITY_DEFINE_INSTANCED_PROP(real, _Frensel)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

real4 GetMask(real2 uv) {
    return SAMPLE_TEXTURE2D(_MaskTex, sampler_MainTex, uv);
}

real2 TransformBaseUV(real2 uv) {
    real4 baseUV_ST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    return uv * baseUV_ST.xy + baseUV_ST.zw;
}

real4 GetBaseColor(real2 baseUV) {
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
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

real GetSmoothness(real2 baseUV = 0.0) {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness) * GetMask(baseUV).a;
}

real GetOccullsion(real2 uv = 0.0) {
    return lerp(1.0, SAMPLE_TEXTURE2D(_MaskTex, sampler_MainTex, uv).g, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Occllusion));
}

real GetFrensel() {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Frensel);
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


#endif // SURFACE_INPUT_UNLIT



#endif //SRP_SHADER_LIBRARY_SURFACE_INPUT