Shader "Custom RP/Lit"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _CutOff ("Alpha Cut Off", Range(0, 1)) = 0.1
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
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
            //Cull Front
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _ RENDER_MODE_CUTOFF

            #include "../ShaderLibrary/ShadowCasterPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode"="CustomLit" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _ RENDER_MODE_CUTOFF RENDER_MODE_FADE RENDER_MODE_TRANSPARENT

            #define MAX_NUM_DIR_LIGHT 4

            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
            UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
            UNITY_DEFINE_INSTANCED_PROP(real4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(real, _CutOff)
            UNITY_DEFINE_INSTANCED_PROP(real, _Metallic)
            UNITY_DEFINE_INSTANCED_PROP(real, _Smoothness)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            // lights
            CBUFFER_START(CustomLight)
            real3 _DirLightColors[MAX_NUM_DIR_LIGHT];
            real3 _DirLightDirections[MAX_NUM_DIR_LIGHT];
            real4 _DirLightShadowData[MAX_NUM_DIR_LIGHT];
            int _DirLightShadowTileIndices[MAX_NUM_DIR_LIGHT];
            int _DirLightCount;
            CBUFFER_END

            // shadows
            #define SHADOW_SAMPLER sampler_linear_clamp_compare
            TEXTURE2D_SHADOW(_DirectioanlShadowAtlas);
            SAMPLER_CMP(SHADOW_SAMPLER);
    
            CBUFFER_START(CustomShaow)
            float4x4 _DirectionalShadowMatrixs[MAX_NUM_DIR_LIGHT];
            CBUFFER_END

            struct Attributes
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 posL : POSITION;
                float3 normalL : NORMAL;
                float2 uv : TEXCOORD;
            };

            struct Varyings
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 posH : SV_POSITION;
                float3 normalW : NORMAL;
                float3 posW : COLOR;
                float2 uv : TEXCOORD;
            };


            Varyings vert (Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float4 uv_st = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
                output.posW = TransformObjectToWorld(input.posL);
                output.posH = TransformWorldToHClip(output.posW);
                output.normalW = TransformObjectToWorldNormal(input.normalL);
                output.uv = input.uv * uv_st.xy + uv_st.zw;
                
                return output;
            }

            real4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                real4 Col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
                
                #if defined(RENDER_MODE_CUTOFF)
                clip(Col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff));
                #endif

                Surface sur;
                sur.color = Col.rgb;
                sur.alpha = Col.a;
                sur.normal = input.normalW;
                sur.viewDirection = normalize(_WorldSpaceCameraPos - input.posW);
                sur.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
                sur.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
                
                real3 finalCol;
                for (int i=0; i<_DirLightCount; i++) {
                    Light light;
                    light.color = _DirLightColors[i];
                    light.direction = _DirLightDirections[i];
                    light.attenuation = 1;

                    int shadowTileIndex = _DirLightShadowTileIndices[i];
                    if ( shadowTileIndex >= 0) { 
                        real shadowStren = _DirLightShadowData[i].x;
                        real4 posShadowed = mul(_DirectionalShadowMatrixs[shadowTileIndex], float4(input.posW, 1));
                        light.attenuation = lerp(1, SAMPLE_TEXTURE2D_SHADOW(_DirectioanlShadowAtlas, SHADOW_SAMPLER, posShadowed), shadowStren);
                    }

                    finalCol += ComputeLighting(sur, light);
                } 

                return real4(finalCol, sur.alpha);
            }

            ENDHLSL
        }
    }

    CustomEditor "LitShaderEditor"
}
