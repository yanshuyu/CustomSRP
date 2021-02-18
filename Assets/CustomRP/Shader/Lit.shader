Shader "Custom RP/Lit"
{
    Properties
    {
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

            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/Lighting.hlsl"
            #include "../ShaderLibrary/GI.hlsl"
            #include "../ShaderLibrary/Shadow.hlsl"
            #include "../shaderLibrary/SurfaceInput.hlsl"
  

            // lights
            CBUFFER_START(CustomLight)
            real3 _DirLightColors[MAX_NUM_DIR_LIGHT];
            real3 _DirLightDirections[MAX_NUM_DIR_LIGHT];
            real4 _DirLightShadowData[MAX_NUM_DIR_LIGHT];
            int _DirLightCount;
            CBUFFER_END


            struct Attributes
            {
                float4 posL : POSITION;
                float3 normalL : NORMAL;
                float2 uv : TEXCOORD;
                UNITY_VERTEX_INPUT_GI_UV
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 posH : SV_POSITION;
                float3 normalW : VAR_NORMAL;
                float3 posW : VAR_POSITION;
                float2 uv : TEXCOORD;
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
                output.uv = TransformBaseUV(input.uv);
                
                return output;
            }

            real4 frag (Varyings input) : SV_Target
            {
                #if defined(LOD_FADE_CROSSFADE)
                ClipLOD(input.posH, unity_LODFade);
                #endif

                UNITY_SETUP_INSTANCE_ID(input);
                real4 Col = GetBaseColor(input.uv);
                
                #if defined(RENDER_MODE_CUTOFF)
                clip(Col.a - GetCutOff());
                #endif

                Surface sur;
                sur.color = Col.rgb;
                sur.alpha = Col.a;
                sur.normal = input.normalW;
                sur.position = input.posW;
                sur.viewDirection = normalize(_WorldSpaceCameraPos - input.posW);
                sur.metallic = GetMetallic(input.uv);
                sur.smoothness = GetSmoothness(input.uv);
                sur.frensel = GetFrensel();
                sur.occllusion = GetOccullsion(input.uv);

                BRDF brdf = GetBRDF(sur.color, sur.metallic, sur.smoothness);
                GI gi = GetGI(sur, UNITY_ACCESS_GI_UV(input));

                real4 finalCol;
                finalCol.a = sur.alpha;
                finalCol.rgb = ComputeIndirectLight(sur, brdf, gi);

                for (int i=0; i<_DirLightCount; i++) {
                    Light light;
                    light.color = _DirLightColors[i];
                    light.direction = _DirLightDirections[i];
                    light.attenuation = ComputeShadowAttenuation(sur, _DirLightShadowData[i], gi);

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
