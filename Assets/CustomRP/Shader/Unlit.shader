Shader "Custom RP/Unlit"
{
    Properties
    {   
        _MainTex ("Main Map(RGBA)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _CutOff ("Alpha Cut Off", Range(0, 1)) = 0.1
        _EmissiveTex ("Emission Map(RGB)", 2D) = "white" {}
        [HDR] _Emission ("Emission", Color) = (0, 0, 0, 1)
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

            #define SURFACE_INPUT_UNLIT

            #include "../ShaderLibrary/MetaPass.hlsl"

            ENDHLSL

        }

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _ RENDER_MODE_CUTOFF RENDER_MODE_FADE

            #define SURFACE_INPUT_UNLIT

            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/SurfaceInput.hlsl"


            struct Attributes
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 posL : POSITION;
                float2 uv : TEXCOORD;
            };

            struct Varyings
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 posH : SV_POSITION;
                float2 uv : TEXCOORD;
            };


            Varyings vert (Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
            
                output.posH = TransformObjectToHClip(input.posL);
                output.uv = TransformBaseUV(input.uv);
                
                return output;
            }

            real4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                real4 Col = GetBaseColor(input.uv);
                
                #if defined(RENDER_MODE_CUTOFF)
                clip(Col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff));
                #endif

                Col.rgb += GetEmission(input.uv);
                
                return Col;
            }

            ENDHLSL
        }
    }

    CustomEditor "UnlitShaderEditor"
}
