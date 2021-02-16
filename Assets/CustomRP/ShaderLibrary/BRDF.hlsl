#ifndef SRP_SHADER_LIBRARY_BRDF
#define SRP_SHADER_LIBRARY_BRDF

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#define MINIMUM_REFLECTIVITY 0.04

struct BRDF {
    real3 diffuse;
    real3 specular;
    real rougness;
};


real GetOneMinusReflectivity(real metallic) {
    real refltRange = 1 - MINIMUM_REFLECTIVITY;
    return refltRange - metallic * refltRange;
}


BRDF GetBRDF(real3 albedo, real metallic, real smoothness) {
    BRDF brdf;
    ZERO_INITIALIZE(BRDF, brdf);
    real oneMinusReflectivity = GetOneMinusReflectivity(metallic);
    brdf.diffuse = albedo * oneMinusReflectivity;
    real3 baseRefltColor = real3(MINIMUM_REFLECTIVITY, MINIMUM_REFLECTIVITY, MINIMUM_REFLECTIVITY);
    brdf.specular = lerp(baseRefltColor, albedo, metallic);
    brdf.rougness = PerceptualRoughnessToRoughness( PerceptualSmoothnessToPerceptualRoughness(smoothness) );
    
    return brdf;
}


#endif