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

        Pass
        {
            HLSLPROGRAM
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

            struct appdata
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float2 uv : TEXCOORD;
            };

            struct v2f
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD;
            };


            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float4 uv_st = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv * uv_st.xy + uv_st.zw;
                
                return o;
            }

            real4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                real4 Col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
                
                #if defined(RENDER_MODE_CUTOFF)
                clip(UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff) - Col.a);
                #endif
                
                return Col;
            }

            ENDHLSL
        }
    }

    CustomEditor "UnlitShaderEditor"
}
