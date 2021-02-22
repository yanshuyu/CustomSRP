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


real GetRangeAttenuation(real3 ray, real range) {
    real distanceOverRange2 = dot(ray, ray) / max(range * range, 0.00001);
    real atten = saturate(1.0 - distanceOverRange2 * distanceOverRange2);
    return  atten * atten; 
}

real GetAngleAttenuation(real3 lightDir, real3 lightFwdDir, real4 spotAngles) {
    real cosHalfInnerAngle = cos(0.5 * spotAngles.x);
    real cosHalfOuterAngle = cos(0.5 * spotAngles.y);
    real dirDot = dot(lightDir, lightFwdDir);
    real atten = saturate((dirDot - cosHalfOuterAngle) / (cosHalfInnerAngle - cosHalfOuterAngle));
    return atten * atten;
}


real3 ComputeLighting(Surface sur, Light light, BRDF brdf) {
    if (light.attenuation <= 0)
        return 0.0;

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
    real3 diffuse = brdf.diffuse * gi.diffuse;
    real3 spec = gi.specular * lerp(brdf.specular, frensel, frenselStren);
    return (diffuse + spec) * sur.occllusion;
}

#endif