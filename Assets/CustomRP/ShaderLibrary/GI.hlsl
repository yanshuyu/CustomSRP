#ifndef SRP_SHADER_LIBRARY_GI
#define SRP_SHADER_LIBRARY_GI

#include "Surface.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

#if defined(LIGHTMAP_ON)
#define UNITY_VERTEX_INPUT_GI_UV real2 lightMapUV : TEXCOORD1;
#define UNITY_VERTEX_VARYING_GI_UV real2 lightMapUV : VAR_LIGHT_MAP_UV;
#define UNITY_TRANSFER_GI_UV(input, output) output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#define UNITY_ACCESS_GI_UV(input) input.lightMapUV

#else
#define UNITY_VERTEX_INPUT_GI_UV
#define UNITY_VERTEX_VARYING_GI_UV
#define UNITY_TRANSFER_GI_UV(input, output)
#define UNITY_ACCESS_GI_UV(input) 0.0

#endif


TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0); // skybox


struct GI {
    real3 diffuse;
    real3 specular;
    real4 bakedShadow; // 4 light baked shadow mask
};


real3 SampleLightMap(real2 lightMapUV) {
    #if !defined(LIGHTMAP_ON)
        return 0.0;
    #else

    return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), 
                                lightMapUV, 
                                float4(1, 1, 0, 0), // transform uv
                                #if defined(UNITY_LIGHTMAP_FULL_HDR) // lightmap is compressed ?
                                    false,
                                #else 
                                    true,
                                #endif
                                float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0 ,0)
                                );
    #endif
}


real3 SampleLightProbes(real3 normal, real3 position) {
    #if defined(LIGHTMAP_ON)
        return 0.0;
    #else 
    	if (unity_ProbeVolumeParams.x) {  // sample light probe proxy volume
			return SampleProbeVolumeSH4( TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
                                        position, normal,
                                        unity_ProbeVolumeWorldToObject,
                                        unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
                                        unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz );
		} else {  // sample light probe
            float4 coefficients[7];
            coefficients[0] = unity_SHAr;
            coefficients[1] = unity_SHAg;
            coefficients[2] = unity_SHAb;
            coefficients[3] = unity_SHBr;
            coefficients[4] = unity_SHBg;
            coefficients[5] = unity_SHBb;
            coefficients[6] = unity_SHC;
            
            return  max(0.0, SampleSH9(coefficients, normal));
        }
    #endif
}


// baked shadow
real4 SampleShadowMask(real2 lightMapUV, real3 position) {
    #if defined(LIGHTMAP_ON)
    return SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, lightMapUV);
    #else 
    if (unity_ProbeVolumeParams.x) {  // sample occlusion probes(light probes) volume
			return SampleProbeOcclusion( TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
                                        position,
                                        unity_ProbeVolumeWorldToObject,
                                        unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
                                        unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz );
    } else {
        return unity_ProbesOcclusion;
    }
    #endif
}


real3 SampleEnvironment(real3 viewDir, real3 normal, real smoothness) {
    real3 uvw = reflect(-viewDir, normal);
    real perceptRougness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
    real4 env = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, uvw, PerceptualRoughnessToMipmapLevel(perceptRougness));
    return DecodeHDREnvironment(env, unity_SpecCube0_HDR);
}


real4 GetBakedShadow(real2 lightMapUV, real3 position) {
    #if defined(SHADOW_MASK_DISTANCE) || defined(SHADOW_MASK_ALWAYS)
        return SampleShadowMask(lightMapUV, position);
    #else 
        return 1.0;
    #endif
}


GI GetGI(Surface sur, real2 lightMapUV) {
    GI gi;
    gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbes(sur.normal, sur.position);
    gi.specular = SampleEnvironment(sur.viewDirection, sur.normal, sur.smoothness);
    gi.bakedShadow = GetBakedShadow(lightMapUV, sur.position);
    return gi;
}


#endif
