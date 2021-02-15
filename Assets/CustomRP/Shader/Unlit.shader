Shader "Custom RP/Unlit"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _CutOff ("Alpha Cut Off", Range(0, 1)) = 0.1
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

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _ RENDER_MODE_CUTOFF RENDER_MODE_FADE

            #include "../ShaderLibrary/Common.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
            UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
            UNITY_DEFINE_INSTANCED_PROP(real4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(real, _CutOff)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

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
                float4 uv_st = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
                output.posH = TransformObjectToHClip(input.posL);
                output.uv = input.uv * uv_st.xy + uv_st.zw;
                
                return output;
            }

            real4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                real4 Col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
                
                #if defined(RENDER_MODE_CUTOFF)
                clip(Col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff));
                #endif
                
                return Col;
            }

            ENDHLSL
        }
    }

    CustomEditor "UnlitShaderEditor"
}
