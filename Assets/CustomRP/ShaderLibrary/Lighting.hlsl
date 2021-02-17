#ifndef SRP_SHADER_LIBRARY_LIGHTING
#define SRP_SHADER_LIBRARY_LIGHTING

#include "Surface.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GI.hlsl"


// specStren = r^2 / (d^2 * max(0.1, LdotH^2) * k )
// r = rougness
// d = NdotH^2 * (r^2 - 1) + 1.001
// k = 4r + 2
real GetBRDFSpecStren(real3 normal, real3 viewDir, real3 lightDir, real rougness) {
    real3 H = normalize(viewDir + lightDir);
    real NdotH = saturate(dot(normal, H));
    real LdotH = saturate(dot(lightDir, H));
    
    real rougness2 = rougness * rougness;
    real d = NdotH * NdotH * (rougness2 - 1) + 1.001;
    real k = 4 * rougness + 2;
    
    return rougness2 / (d*d * max(0.1, LdotH * LdotH) * k);
}


void GetFresel(Surface sur, out real frensel, out real frenselStren) {
    real oneMinusReflectivity = GetOneMinusReflectivity(sur.metallic);
    frensel = saturate(sur.smoothness + 1.0 - oneMinusReflectivity);
    frenselStren = Pow4(1.0 - saturate( dot(sur.viewDirection, sur.normal) ) );
}


real3 ComputeLighting(Surface sur, Light light, BRDF brdf) {
    real3 incomingLight = light.color * saturate(dot(light.direction, sur.normal)) * light.attenuation;

    #if defined(RENDER_MODE_TRANSPARENT) // pre multiply alpha
    diffuse *= sur.alpha;
    #endif

    real specStren = GetBRDFSpecStren(sur.normal, sur.viewDirection, light.direction, brdf.rougness);

    real3 col = incomingLight * (brdf.diffuse + brdf.specular * specStren);
   
    return col;
}



real3 ComputeIndirectLight(Surface sur, BRDF brdf, GI gi) {
    real frensel, frenselStren; 
    GetFresel(sur, frensel, frenselStren);
    frenselStren *= sur.frensel;
    return brdf.diffuse * gi.diffuse + gi.specular * lerp(brdf.specular, frensel, frenselStren);
}

#endif