Shader "Custom RP/Lit"
{
    Properties
    {
        // Base
        _MainTex ("Main Map(RGBA)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _CutOff ("Alpha Cut Off", Range(0, 1)) = 0.1
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Frensel ("Frensel", Range(0, 1)) = 1.0
        _EmissiveTex ("Emission Map(RGB)", 2D) = "white" {}
        [HDR] _Emission ("Emission", Color) = (0, 0, 0, 1)
        _MaskTex ("MODS Map(Metallic/Occlusion/Detail/Smoothness Mask(RGBA))", 2D) = "white" {}
        _Occllusion ("Occllusion", Range(0, 1)) = 1
        _NormalTex ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 1)) = 1

        // Detail
        _DetailTex ("Detail Map(R(Aldedo) B(Smoothness), AG(Normal))", 2D) = "gray" {}
        _DetailNormalTex ("Detail Normal Map", 2D) = "bump" {}
        _DetailAlbedo ("Detail Albedo", Range(0, 1)) = 1
        _DetailSmoothness ("Detail Smoothness", Range(0, 1)) = 1
        _DetailNormalScale ("Detail Normal Scale", Range(0, 1)) = 1

        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        LOD 100
        ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]

        Pass {
            Tags {"LightMode"="ShadowCaster"}
            ColorMask 0
            Cull Front
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma shader_feature _ RENDER_MODE_CUTOFF

            #include "../ShaderLibrary/ShadowCasterPass.hlsl"

            ENDHLSL
        }

        Pass {
            Tags {"LightMode"="Meta"}
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ RENDER_MODE_CUTOFF

            #include "../ShaderLibrary/MetaPass.hlsl"

            ENDHLSL

        }

        Pass
        {
            Tags { "LightMode"="CustomLit" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ SHADOW_MASK_DISTANCE SHADOW_MASK_ALWAYS
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma shader_feature _ RENDER_MODE_CUTOFF RENDER_MODE_FADE RENDER_MODE_TRANSPARENT

            #define MAX_NUM_DIR_LIGHT 4
            #define MAX_NUM_POINT_LIGHT 16
            #define MAX_NUM_SPOT_LIGHT 16

            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/Lighting.hlsl"
            #include "../ShaderLibrary/GI.hlsl"
            #include "../ShaderLibrary/Shadow.hlsl"
            #include "../shaderLibrary/SurfaceInput.hlsl"
  

            // lights
            CBUFFER_START(CustomLight)
            real4 _DirLightColors[MAX_NUM_DIR_LIGHT];  // directional lights
            real4 _DirLightDirections[MAX_NUM_DIR_LIGHT];
            real4 _DirLightShadowData[MAX_NUM_DIR_LIGHT];
            int _DirLightCount;

            real4 _PointLightColors[MAX_NUM_POINT_LIGHT]; // point lights
            real4 _PointLightPositions[MAX_NUM_POINT_LIGHT];
            real4 _PointLightShadowData[MAX_NUM_POINT_LIGHT];
            int _PointLightCount;
            
            real4 _SpotLightColors[MAX_NUM_SPOT_LIGHT]; // spot lights
            real4 _SpotLightPositions[MAX_NUM_SPOT_LIGHT];
            real4 _SpotLightDirections[MAX_NUM_SPOT_LIGHT];
            real4 _SpotLightAngles[MAX_NUM_SPOT_LIGHT];
            real4 _SpotLightShadowData[MAX_NUM_SPOT_LIGHT];
            int _SpotLightCount;
            CBUFFER_END


            struct Attributes
            {
                float4 posL : POSITION;
                float4 tangentL : TANGENT;
                float3 normalL : NORMAL;
                float2 uv : TEXCOORD;
                UNITY_VERTEX_INPUT_GI_UV
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 posH : SV_POSITION;
                float4 tangentW : VAR_TANGENT;
                float3 normalW : VAR_NORMAL;
                float3 posW : VAR_POSITION;
                float2 uv : VAR_BASE_UV;
                float2 detailUV : VAR_DETAIL_UV;
                UNITY_VERTEX_VARYING_GI_UV
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            Varyings vert (Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_TRANSFER_GI_UV(input, output)
                
                output.posW = TransformObjectToWorld(input.posL);
                output.posH = TransformWorldToHClip(output.posW);
                output.normalW = TransformObjectToWorldNormal(input.normalL);
                output.tangentW = float4(TransformObjectToWorldDir(input.tangentL.xyz), input.tangentL.w);
                output.uv = TransformBaseUV(input.uv);
                output.detailUV = TransformDetailUV(input.uv);

                return output;
            }

            real4 frag (Varyings input) : SV_Target
            {
                #if defined(LOD_FADE_CROSSFADE)
                ClipLOD(input.posH, unity_LODFade);
                #endif

                UNITY_SETUP_INSTANCE_ID(input);
                real4 Col = GetBaseColor(input.uv, input.detailUV);
                
                #if defined(RENDER_MODE_CUTOFF)
                clip(Col.a - GetCutOff());
                #endif

                Surface sur;
                sur.color = Col.rgb;
                sur.alpha = Col.a;
                sur.normal = NormalTangentToWorld(GetNormalTS(input.uv), input.normalW, input.tangentW.xyz, input.tangentW.w);
                sur.position = input.posW;
                sur.viewDirection = normalize(_WorldSpaceCameraPos - input.posW);
                sur.metallic = GetMetallic(input.uv);
                sur.smoothness = GetSmoothness(input.uv, input.detailUV);
                sur.frensel = GetFrensel();
                sur.occllusion = GetOccullsion(input.uv);

                BRDF brdf = GetBRDF(sur.color, sur.metallic, sur.smoothness);
                GI gi = GetGI(sur, UNITY_ACCESS_GI_UV(input));

                real4 finalCol;
                finalCol.a = sur.alpha;
                finalCol.rgb = ComputeIndirectLight(sur, brdf, gi);

                Light light;
                ZERO_INITIALIZE(Light, light);
                for (int i=0; i<_DirLightCount; i++) {
                    light.color = _DirLightColors[i];
                    light.direction = _DirLightDirections[i];
                    light.attenuation = GetDirectionalShadowAtten(_DirLightShadowData[i], sur, light, gi);
                    finalCol.rgb += ComputeLighting(sur, light, brdf);
                }

                ZERO_INITIALIZE(Light, light);
                for (int j=0; j<_PointLightCount; j++) {
                    real3 sur2Light = _PointLightPositions[j].xyz - sur.position;
                    light.color = _PointLightColors[j];
                    light.direction = normalize(sur2Light);
                    light.attenuation = GetRangeAttenuation(sur2Light, _PointLightPositions[j].w) 
                                        * GetPointShadowAtten(_PointLightShadowData[j], sur, light);
                    finalCol.rgb += ComputeLighting(sur, light, brdf);
                }

                ZERO_INITIALIZE(Light, light);
                for (int k=0; k<_SpotLightCount; k++) {
                    real3 sur2Light = _SpotLightPositions[k].xyz - sur.position;
                    light.color = _SpotLightColors[k];
                    light.direction = normalize(sur2Light);
                    light.attenuation = GetRangeAttenuation(sur2Light, _SpotLightPositions[k].w) 
                                        * GetAngleAttenuation(light.direction, _SpotLightDirections[k], _SpotLightAngles[k])
                                        * GetSpotShadowAtten(_SpotLightShadowData[k], sur, light);
                    finalCol.rgb += ComputeLighting(sur, light, brdf);
                }

                finalCol.rgb += GetEmission(input.uv);
        
                return finalCol;
            }

            ENDHLSL
        }
    }

    CustomEditor "LitShaderEditor"
}
