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

        Pass
        {
            Tags { "LightMode"="CustomLit" }

            HLSLPROGRAM
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

            CBUFFER_START(CustomLight)
            real3 _DirLightColors[MAX_NUM_DIR_LIGHT];
            real3 _DirLightDirections[MAX_NUM_DIR_LIGHT];
            int _DirLightCount;
            CBUFFER_END

            struct appdata
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 posL : POSITION;
                float3 normalL : NORMAL;
                float2 uv : TEXCOORD;
            };

            struct v2f
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 posH : SV_POSITION;
                float3 normalW : NORMAL;
                float3 posW : COLOR;
                float2 uv : TEXCOORD;
            };


            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float4 uv_st = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
                o.posW = TransformObjectToWorld(v.posL);
                o.posH = TransformWorldToHClip(o.posW);
                o.normalW = TransformObjectToWorldNormal(v.normalL);
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

                Surface sur;
                sur.color = Col.rgb;
                sur.alpha = Col.a;
                sur.normal = i.normalW;
                sur.viewDirection = normalize(_WorldSpaceCameraPos - i.posW);
                sur.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
                sur.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
                
                real3 finalCol;
                for (int i=0; i<_DirLightCount; i++) {
                    Light l;
                    l.color = _DirLightColors[i];
                    l.direction = _DirLightDirections[i];
                    finalCol += ComputeLighting(sur, l);
                } 

                return real4(finalCol, sur.alpha);
            }

            ENDHLSL
        }
    }

    CustomEditor "LitShaderEditor"
}
