#ifndef SRP_SHADER_LIBRARY_LIGHTING
#define SRP_SHADER_LIBRARY_LIGHTING

#include "Surface.hlsl"
#include "Light.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#define MINIMUM_REFLECTIVITY 0.04

real GetOneMinusReflectivity(real metallic) {
    real refltRange = 1 - MINIMUM_REFLECTIVITY;
    return refltRange - metallic * refltRange;
}

// specStren = r^2 / (d^2 * max(0.1, LdotH^2) * k )
// r = rougness
// d = NdotH^2 * (r^2 - 1) + 1.001
// k = 4r + 2
real GetSpecStren(real rougness, real NdotH, real LdotH) {
    real rougness2 = rougness * rougness;
    real d = NdotH * NdotH * (rougness2 - 1) + 1.001;
    real k = 4 * rougness + 2;
    
    return rougness2 / (d*d * max(0.1, LdotH * LdotH) * k);
}

void GetBRDF(in Surface sur, in Light light, out real3 diffuse, out real3 spec) {
    real oneMinusReflectivity = GetOneMinusReflectivity(sur.metallic);
    diffuse = sur.color * oneMinusReflectivity;
    
    real3 H = normalize(sur.viewDirection + light.direction);
    real NdotH = saturate(dot(sur.normal, H));
    real LdotH = saturate(dot(light.direction, H));
    real rougness = PerceptualRoughnessToRoughness( PerceptualSmoothnessToPerceptualRoughness(sur.smoothness) );
    real3 baseRefltColor = real3(MINIMUM_REFLECTIVITY, MINIMUM_REFLECTIVITY, MINIMUM_REFLECTIVITY);
    spec = lerp(baseRefltColor, sur.color, sur.metallic) * GetSpecStren(rougness, NdotH, LdotH);
}

real3 ComputeLighting(Surface sur, Light light) {
    real3 incomingLight = light.color * saturate(dot(light.direction, sur.normal));
    
    real3 diffuse, spec;
    GetBRDF(sur, light, diffuse, spec);
    
    #if defined(RENDER_MODE_TRANSPARENT) // pre multiply alpha
    diffuse *= sur.alpha;
    #endif

    real3 col = incomingLight * (diffuse + spec);
   
    return col;
}

#endif