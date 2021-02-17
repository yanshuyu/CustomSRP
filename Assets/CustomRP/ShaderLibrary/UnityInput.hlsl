#ifndef SRP_SHADER_LIBRARY_UNITY_INPUT
#define SRP_SHADER_LIBRARY_UNITY_INPUT

CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;
float4 unity_WorldTransformParams;

float4 unity_LightmapST; // lightmaps
float4 unity_DynamicLightmapST;

float4 unity_ProbesOcclusion; // baked shadow mask 

float4 unity_SHAr; // light probes
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;

float4 unity_ProbeVolumeParams; // light probe proxy volume
float4x4 unity_ProbeVolumeWorldToObject;
float4 unity_ProbeVolumeSizeInv;
float4 unity_ProbeVolumeMin;

CBUFFER_END

float3 _WorldSpaceCameraPos;

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;


#endif