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
            Cull Front
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
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
            #pragma multi_compile_instancing
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
            int _DirLightShadowTileIndices[MAX_NUM_DIR_LIGHT];
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
                UNITY_SETUP_INSTANCE_ID(input);
                real4 Col = GetBaseColor(input.uv);
                
                #if defined(RENDER_MODE_CUTOFF)
                clip(Col.a - GetCutOff());
                #endif

                Surface sur;
                sur.color = Col.rgb;
                sur.alpha = Col.a;
                sur.normal = input.normalW;
                sur.viewDirection = normalize(_WorldSpaceCameraPos - input.posW);
                sur.metallic = GetMetallic(input.uv);
                sur.smoothness = GetSmoothness(input.uv);

                BRDF brdf = GetBRDF(sur.color, sur.metallic, sur.smoothness);
                GI gi = ComputeGI(UNITY_ACCESS_GI_UV(input), input.normalW, input.posW);

                real4 finalCol;
                finalCol.a = sur.alpha;
                finalCol.rgb = brdf.diffuse * gi.diffuse;

                for (int i=0; i<_DirLightCount; i++) {
                    Light light;
                    light.color = _DirLightColors[i];
                    light.direction = _DirLightDirections[i];
                    light.attenuation = ComputeShadowAttenuation(_DirLightShadowTileIndices[i], _DirLightShadowData[i].x, _DirLightShadowData[i].y, input.posW, input.normalW);

                    finalCol.rgb += ComputeLighting(sur, light, brdf);
                } 

        
                return finalCol;
            }

            ENDHLSL
        }
    }

    CustomEditor "LitShaderEditor"
}
